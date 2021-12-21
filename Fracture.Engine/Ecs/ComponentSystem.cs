using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fracture.Common.Collections;
using Fracture.Common.Events;
using Fracture.Engine.Core;
using Fracture.Engine.Events;

namespace Fracture.Engine.Ecs
{
   public readonly struct ComponentEventArgs
   {
      #region Properties
      public int Id
      {
         get;
      }
      #endregion

      public ComponentEventArgs(int id)
      {
         Id = id;
      }
   }
   
   /// <summary>
   /// Interface for implementing component systems. Components in Minima
   /// are represented as identifiers and systems associate them with data
   /// and operations.
   /// </summary>
   public interface IComponentSystem : IObjectManagementSystem, IEnumerable<int>
   {
      #region Properties
      /// <summary>
      /// Event invoked when component was deleted.
      /// </summary>
      IEvent<int, ComponentEventArgs> Deleted
      {
         get;
      }
      #endregion
      
      /// <summary>
      /// Returns boolean declaring whether a component
      /// with given id is alive.
      /// </summary>
      bool Alive(int id);
      
      /// <summary>
      /// Returns count of components for given entity.
      /// </summary>
      int BoundTo(int entityId);
      
      int FirstFor(int entityId);
      int AtIndex(int entityId, int index);
      
      IEnumerable<int> IndicesOf(int entityId);
      
      /// <summary>
      /// Attempts to delete component with given id, returns
      /// boolean declaring whether the component was deleted.
      /// </summary>
      bool Delete(int id);
   }
   
   /// <summary>
   /// Abstract base class for all component systems.
   /// </summary>
   public abstract class ComponentSystem : GameEngineSystem, IComponentSystem
   {
      #region Fields
      /// <summary>
      /// Mappings that maps entity id to component id.
      /// </summary>
      private readonly Dictionary<int, int> componentToEntityMap;
      
      private readonly FreeList<int> freeComponents;
      
      private readonly List<int> aliveComponents;
      
      private readonly IEventQueue<int, ComponentEventArgs> deletedEvents;
      
      private readonly IEntitySystem entities;
      #endregion

      #region Properties
      protected int Count => aliveComponents.Count;
      
      public IEvent<int, ComponentEventArgs> Deleted
         => deletedEvents;
      #endregion

      protected ComponentSystem(IEntitySystem entities, IEventQueueSystem events)
      {
         this.entities = entities ?? throw new ArgumentNullException(nameof(entities));
         
         deletedEvents = events.CreateShared<int, ComponentEventArgs>();
         
         // Create basic component data.
         var idc = 0;
         
         freeComponents       = new FreeList<int>(() => idc++);
         aliveComponents      = new List<int>();
         componentToEntityMap = new Dictionary<int, int>();
      }
      
      protected void AssertAlive(int id)
      {
#if DEBUG || ECS_RUNTIME_CHECKS
         if (!Alive(id)) 
            throw new InvalidOperationException($"component {id} does not exist");
#endif
      }
      
      protected int OwnerOf(int id) 
         => componentToEntityMap[id];
      
      protected int AtIndex(int index)
         => aliveComponents[index];
       
      protected virtual int InitializeComponent(int entityId)
      {
         var id = freeComponents.Take();
         
         // Make sure this component is not duplicated.
         if (componentToEntityMap.ContainsKey(id)) 
            throw new InvalidOperationException($"duplicated internal component id {id}");
         
         aliveComponents.Add(id);
         componentToEntityMap.Add(id, entityId);
         
         // Create events.
         deletedEvents.Create(id);
         
         // Delete the component when entity is deleted.
         entities.Deleted.Subscribe(entityId, delegate
         {
            if (!Alive(id))
               return;
            
            Delete(id);
         });
         
         return id;
      }
      
      public void AssertHasSingle(int entityId)
      {
#if DEBUG || ECS_RUNTIME_CHECKS
         if (BoundTo(entityId) != 1)
            throw new InvalidOperationException($"expecting one component to be present for entity {entityId}");
#endif
      }
      
      public void AssertHasMultiple(int entityId)
      {
#if DEBUG || ECS_RUNTIME_CHECKS
         if (BoundTo(entityId) < 2)
            throw new InvalidOperationException($"expecting more than one component to be present for entity {entityId}");
#endif
      }
      
      public void AssertHasNone(int entityId)
      {
#if DEBUG || ECS_RUNTIME_CHECKS
         if (BoundTo(entityId) != 0)
            throw new InvalidOperationException($"expecting no component to be present for entity {entityId}");
#endif
      }
      
      public virtual bool Delete(int id)
      {
         if (!componentToEntityMap.Remove(id))
            return false;
         
         // Publish deleted event.
         deletedEvents.Publish(id, new ComponentEventArgs(id));

         aliveComponents.Remove(id);
         componentToEntityMap.Remove(id);
         
         freeComponents.Return(id);
         
         return true;
      }

      public bool Alive(int id)
         => componentToEntityMap.ContainsKey(id);

      public abstract int BoundTo(int entityId);
      public abstract int FirstFor(int entityId);
      public abstract int AtIndex(int entityId, int index);
      public abstract IEnumerable<int> IndicesOf(int entityId);
      
      public virtual void Clear()
      {
         while (aliveComponents.Count != 0)
            Delete(aliveComponents[0]);
      }

      public IEnumerator<int> GetEnumerator()
         => aliveComponents.GetEnumerator();
      
      IEnumerator IEnumerable.GetEnumerator()
         => GetEnumerator();
   }
   
   /// <summary>
   /// Component system that allows one component per entity to exist.
   /// </summary>
   public abstract class UniqueComponentSystem : ComponentSystem
   {
      #region Fields
      /// <summary>
      /// Lookup that maps entity id to component id.
      /// </summary>
      private readonly Dictionary<int, int> entityToComponentMap;
      #endregion
      
      protected UniqueComponentSystem(IEntitySystem entities, IEventQueueSystem events) 
         : base(entities, events) => entityToComponentMap = new Dictionary<int, int>();
      
      protected override int InitializeComponent(int entityId)
      {
         if (BoundTo(entityId) != 0)
            throw new InvalidOperationException($"entity {entityId} already has unique component");
         
         var id = base.InitializeComponent(entityId);
         
         entityToComponentMap.Add(entityId, id);
         
         return id;
      }
      
      public override bool Delete(int id)
      {
         var success = base.Delete(id);
         
         if (success)
            entityToComponentMap.Remove(id);
         
         return success;
      }

      public sealed override int BoundTo(int entityId)
         => entityToComponentMap.ContainsKey(entityId) ? 1 : 0;
      
      public sealed override int FirstFor(int entityId)
      {
         if (entityToComponentMap.TryGetValue(entityId, out var componentId))
            return componentId;
         
         throw new ComponentNotFoundException(entityId);
      }

      public sealed override int AtIndex(int entityId, int index)
      {
         if (index != 0) 
            throw new IndexOutOfRangeException(nameof(index));
         
         return FirstFor(entityId);
      }

      public override IEnumerable<int> IndicesOf(int entityId)
      {
         // Return first index if entity has component associated with it.
         if (BoundTo(entityId) != 0)
            yield return 0;
      }
   }
   
   /// <summary>
   /// Component system that allows entity to own more than one
   /// component of specific type.
   /// </summary>
   public abstract class SharedComponentSystem : ComponentSystem
   {
      #region Fields
      /// <summary>
      /// Lookup that maps entity id to component id list.
      /// </summary>
      private readonly Dictionary<int, List<int>> entityToComponentsMap;
      #endregion
      
      protected SharedComponentSystem(IEntitySystem entities, IEventQueueSystem events) 
         : base(entities, events) => entityToComponentsMap = new Dictionary<int, List<int>>();
      
      protected override int InitializeComponent(int entityId)
      {
         var id = base.InitializeComponent(entityId);
         
         // TODO: i think we can pool these lists?
         if (!entityToComponentsMap.TryGetValue(entityId, out var components))
         {
            components = new List<int>();
            
            entityToComponentsMap.Add(entityId, components);
         }
         
         components.Add(id);
         
         return id;
      }

      public override bool Delete(int id)
      {
         if (!Alive(id)) 
            return false;
         
         var entityId = OwnerOf(id);
         
         if (!base.Delete(id)) 
            return false;
         
         // Remove from map.
         var components = entityToComponentsMap[entityId];
            
         components.Remove(id);
            
         // Remove map if empty.
         if (components.Count == 0) 
            entityToComponentsMap.Remove(entityId);

         return true;
      }

      public override int BoundTo(int entityId)
         => entityToComponentsMap.TryGetValue(entityId, out var components) ? components.Count : 0;
      
      public override int FirstFor(int entityId)
         => AtIndex(entityId, 0);
      
      public override int AtIndex(int entityId, int index)
      {
         if (BoundTo(entityId) == 0)
            throw new ComponentNotFoundException(entityId);
            
         return entityToComponentsMap[entityId][index];
      }
      
      public override IEnumerable<int> IndicesOf(int entityId)
         => Enumerable.Range(0, BoundTo(entityId));
   }
}