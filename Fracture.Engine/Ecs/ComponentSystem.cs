using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fracture.Common.Collections;
using Fracture.Common.Events;
using Fracture.Common.Memory.Pools;
using Fracture.Common.Memory.Storages;
using Fracture.Engine.Core;
using Fracture.Engine.Events;

namespace Fracture.Engine.Ecs
{
   /// <summary>
   /// Interface for implementing component systems. Components in Minima
   /// are represented as identifiers and systems associate them with data
   /// and operations.
   /// </summary>
   public interface IComponentSystem : IObjectManagementSystem, IEnumerable<int>
   {
      /// <summary>
      /// Returns the id of the entity that owns the component with given id.
      /// </summary>
      int OwnerOf(int id);
      
      /// <summary>
      /// Returns boolean declaring whether a component with given id is alive.
      /// </summary>
      bool IsAlive(int id);
      
      /// <summary>
      /// Returns boolean declaring whether any components are bound to this entity.
      /// </summary>
      bool BoundTo(int entityId);
      
      int FirstFor(int entityId);
      
      IEnumerable<int> AllFor(int entityId);
      
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
      #endregion

      #region Properties
      /// <summary>
      /// Gets the ids of alive components.
      /// </summary>
      protected IEnumerable<int> Alive 
         => aliveComponents;
      
      protected IEventQueueSystem Events
      {
         get;
      }
      #endregion
      
      protected ComponentSystem(IEventQueueSystem events)
      {
         Events = events;
         
         // Create basic component data.
         var idc = 0;
         
         freeComponents       = new FreeList<int>(() => idc++);
         aliveComponents      = new List<int>();
         componentToEntityMap = new Dictionary<int, int>();
      }
      
      protected void AssertAlive(int id)
      {
#if DEBUG || ECS_RUNTIME_CHECKS
         if (!IsAlive(id)) 
            throw new InvalidOperationException($"component {id} does not exist");
#endif
      }
      
      /// <summary>
      /// Gets the id of component at given index.
      /// </summary>
      protected int IdAtIndex(int index)
         => aliveComponents[index];
      
      protected virtual int InitializeComponent(int entityId)
      {
         var id = freeComponents.Take();
         
         // Make sure this component is not duplicated.
         if (componentToEntityMap.ContainsKey(id)) 
            throw new InvalidOperationException($"duplicated internal component id {id}");
         
         aliveComponents.Add(id);
         componentToEntityMap.Add(id, entityId);

         return id;
      }
      
      public int OwnerOf(int id) 
         => componentToEntityMap[id];

      public virtual bool Delete(int id)
      {
         if (!componentToEntityMap.Remove(id))
            return false;
         
         aliveComponents.Remove(id);
         componentToEntityMap.Remove(id);
         
         freeComponents.Return(id);
         
         return true;
      }

      public bool IsAlive(int id)
         => componentToEntityMap.ContainsKey(id);

      public abstract bool BoundTo(int entityId);
      public abstract int FirstFor(int entityId);
      public abstract IEnumerable<int> AllFor(int entityId);

      public override void Update(IGameEngineTime time)
      {
         Events.GetEventHandler<int, EntityEventArgs>().Handle((in Letter<int, EntityEventArgs> letter) => 
         {
            foreach (var id in AllFor(letter.Args.EntityId))
               Delete(id);
            
            return LetterHandlingResult.Retain;
         });
      }

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
      
      protected UniqueComponentSystem(IEventQueueSystem events) 
         : base(events) => entityToComponentMap = new Dictionary<int, int>();
      
      protected override int InitializeComponent(int entityId)
      {
         if (BoundTo(entityId))
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

      public sealed override bool BoundTo(int entityId)
         => entityToComponentMap.ContainsKey(entityId);
      
      public sealed override int FirstFor(int entityId)
      {
         if (entityToComponentMap.TryGetValue(entityId, out var componentId))
            return componentId;
         
         throw new ComponentNotFoundException(entityId);
      }

      public override IEnumerable<int> AllFor(int entityId)
      {
         // Return first index if entity has component associated with it.
         if (BoundTo(entityId))
            yield return FirstFor(entityId);
      }
   }
   
   /// <summary>
   /// Component system that allows entity to own more than one
   /// component of specific type.
   /// </summary>
   public abstract class SharedComponentSystem : ComponentSystem
   {
      #region Static fields
      private static readonly CollectionPool<List<int>> EntityComponentListPool;
      #endregion
      
      #region Fields
      /// <summary>
      /// Lookup that maps entity id to component id list.
      /// </summary>
      private readonly Dictionary<int, List<int>> entityToComponentsMap;
      #endregion

      static SharedComponentSystem()
      {
         EntityComponentListPool = new CollectionPool<List<int>>(
            new Pool<List<int>>(new LinearStorageObject<List<int>>(new LinearGrowthArray<List<int>>()))
         );
      }
      
      protected SharedComponentSystem(IEventQueueSystem events) 
         : base(events)
      {
         entityToComponentsMap = new Dictionary<int, List<int>>();
      }
      
      protected override int InitializeComponent(int entityId)
      {
         var id = base.InitializeComponent(entityId);
         
         if (!entityToComponentsMap.TryGetValue(entityId, out var components))
         {
            components = EntityComponentListPool.Take();
            
            entityToComponentsMap.Add(entityId, components);
         }
         
         components.Add(id);
         
         return id;
      }

      public override bool Delete(int id)
      {
         if (!IsAlive(id)) 
            return false;
         
         var entityId = OwnerOf(id);
         
         if (!base.Delete(id)) 
            return false;
         
         // Remove from map.
         var components = entityToComponentsMap[entityId];
            
         components.Remove(id);
            
         // Remove map if empty.
         if (components.Count != 0) 
            return true;
         
         EntityComponentListPool.Return(components);
            
         entityToComponentsMap.Remove(entityId);

         return true;
      }

      public override bool BoundTo(int entityId)
         => entityToComponentsMap.Count != 0;
      
      public override int FirstFor(int entityId)
      {
         if (entityToComponentsMap.TryGetValue(entityId, out var components))
            return components.First();
         
         throw new ComponentNotFoundException(entityId);
      }
      
      public override IEnumerable<int> AllFor(int entityId)
         => entityToComponentsMap.TryGetValue(entityId, out var components) ? components.ToArray() : Array.Empty<int>();
   }
}