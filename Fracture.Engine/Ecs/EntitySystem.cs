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
   /// <summary>
   /// Event handler for handling entity events.
   /// </summary>
   public delegate void EntityEventHandler(int id);
   
   /// <summary>
   /// Event handler for handling pair entity events.
   /// </summary>
   public delegate void EntityPairEventHandler(int parentId, int childId);
   
   /// <summary>
   /// Interface for implementing entity systems. Entity in Minima is
   /// just a identifier and systems associated them with data and operations.
   /// </summary>
   public interface IEntitySystem : IGameEngineSystem, IEnumerable<int>
   {
      #region Properties
      /// <summary>
      /// Event invoked when entity has been deleted.
      /// </summary>
      IEvent<int, EntityEventHandler> Deleted
      {
         get;
      }
      
      /// <summary>
      /// Event invoked when entity has been unpaired from one of it's parents.
      /// </summary>
      IEvent<int, EntityPairEventHandler> UnpairedFromChild
      {
         get;
      }
      
      /// <summary>
      /// Event invoked entity has been unpaired from it's parent.
      /// </summary>
      IEvent<int, EntityPairEventHandler> UnpairedFromParent
      {
         get;
      }
      
      /// <summary>
      /// Event invoked when entity is made parent of other entity.
      /// </summary>
      IEvent<int, EntityPairEventHandler> MadeParentOf
      {
         get;
      }
      
      /// <summary>
      /// Event invoked when entity is made child of other entity.
      /// </summary>
      IEvent<int, EntityPairEventHandler> MadeChildOf
      {
         get;
      }
      #endregion
      
      /// <summary>
      /// Creates new entity with optional annotation and tag.
      /// </summary>
      /// <returns>id of the new entity</returns>
      int Create(uint annotation = 0u, string tag = "");
      /// <summary>
      /// Creates new entity that has it's id explicitly defined.  
      /// </summary>
      /// <returns>boolean declaring whether the entity was created</returns>
      bool Create(int id, uint annotation = 0u, string tag = "");
      /// <summary>
      /// Attempts to delete entity with given id. 
      /// </summary>
      void Delete(int id);
      
      /// <summary>
      /// Returns boolean declaring whether entity with given id
      /// is alive.
      /// </summary>
      bool Alive(int id);
      
      uint GetAnnotation(int id);
      string GetTag(int id);

      void Pair(int parentId, int childId);
      void Unpair(int parentId, int childId);
      
      IEnumerable<int> ParentsOf(int childId);
      IEnumerable<int> ChildrenOf(int parentId);
      
      void Clear();
   }

   /// <summary>
   /// Default implementation of <see cref="IEntitySystem"/>.
   /// </summary>
   public sealed class EntitySystem : GameEngineSystem, IEntitySystem
   {
      #region Fields
      // Free (dead/unused) entities and alive entities. Get id from
      // the free list to create new entity.
      private readonly FreeList<int> freeEntities;
      private readonly HashSet<int> aliveEntities;
      
      // Actual entity data. Use the id of the entity to access
      // it's properties trough these lookup structures.
      private readonly Dictionary<int, string> entityTags;
      private readonly Dictionary<int, uint> entityAnnotations;
      private readonly Dictionary<int, HashSet<int>> entityChildren;
      private readonly Dictionary<int, HashSet<int>> entityParents;
      
      // Entity events. 
      private IEventQueue<int, EntityEventHandler> deletedEvents;
      private IEventQueue<int, EntityPairEventHandler> unpairedFromChildEvents;
      private IEventQueue<int, EntityPairEventHandler> unpairedFromParentEvents;
      private IEventQueue<int, EntityPairEventHandler> madeParentOfEvents;
      private IEventQueue<int, EntityPairEventHandler> madeChildOfEvents;
      #endregion

      #region Properties
      public IEvent<int, EntityEventHandler> Deleted
         => deletedEvents;
      
      public IEvent<int, EntityPairEventHandler> UnpairedFromChild
         => unpairedFromChildEvents;
      
      public IEvent<int, EntityPairEventHandler> UnpairedFromParent
         => unpairedFromParentEvents;
      
      public IEvent<int, EntityPairEventHandler> MadeParentOf
         => madeParentOfEvents;
      
      public IEvent<int, EntityPairEventHandler> MadeChildOf
         => madeChildOfEvents;
      #endregion

      public EntitySystem()
      {
         // Create entity data. 
         var idc = 0;
         
         freeEntities  = new FreeList<int>(() => idc++);
         aliveEntities = new HashSet<int>();
         
         entityTags        = new Dictionary<int, string>();
         entityAnnotations = new Dictionary<int, uint>();
         entityChildren    = new Dictionary<int, HashSet<int>>();
         entityParents     = new Dictionary<int, HashSet<int>>();
      }

      private void AssertAlive(int id)
      {
         if (!Alive(id)) 
            throw new InvalidOperationException($"entity {id} does not exist");
      }
      
      public int Create(uint annotation = 0u, string tag = "")
      {
         var id = freeEntities.Take();
         
         if (!Create(id, annotation, tag))
            throw new InvalidOperationException($"duplicated internal entity id {id}");
         
         return id;
      }
      
      public bool Create(int id, uint annotation = 0u, string tag = "")
      {
         if (Alive(id))
            return false;
         
         // Add to lookups.
         entityAnnotations[id] = annotation;
         entityTags[id]        = tag;
         
         aliveEntities.Add(id);
         
         // Create events.
         deletedEvents.Create(id);
         
         unpairedFromChildEvents.Create(id);
         unpairedFromParentEvents.Create(id);
         
         madeParentOfEvents.Create(id);
         madeChildOfEvents.Create(id);
         
         return true;
      }

      public void Delete(int id)
      {
         if (!aliveEntities.Remove(id)) 
            throw new InvalidOperationException($"entity {id} not alive");
         
         // Notify events.
         deletedEvents.Publish(id, e => e(id));
         
         // Delete and unpair children.
         if (entityChildren.TryGetValue(id, out var children))
         {
            foreach (var childId in children)
            {
               Unpair(id, childId);
               
               Delete(childId);
            }
               
            entityChildren.Remove(id);
         }
         
         // Unpair from parents.
         if (entityParents.TryGetValue(id, out var parents))
         {
            foreach (var parentId in parents)
               Unpair(parentId, id);
 
            parents.Remove(id);
         }
         
         // Clear rest of the state and return id to pool.
         deletedEvents.Delete(id);
         
         unpairedFromChildEvents.Delete(id);
         unpairedFromParentEvents.Delete(id);
         
         madeParentOfEvents.Delete(id);
         madeChildOfEvents.Delete(id);
         
         entityTags.Remove(id);
         entityAnnotations.Remove(id);
         
         freeEntities.Return(id);
      }

      public bool Alive(int id)
         => aliveEntities.Contains(id);

      public uint GetAnnotation(int id)
      {
         AssertAlive(id);
         
         return entityAnnotations[id];
      }

      public string GetTag(int id)
      {
         AssertAlive(id);
         
         return entityTags[id];
      }

      public void Pair(int parentId, int childId)
      {
         AssertAlive(parentId);
         AssertAlive(childId);
         
         // Get or create children collection for parent.
         if (!entityChildren.TryGetValue(parentId, out var children))
         {
            children = new HashSet<int>();
            
            entityChildren.Add(parentId, children);
         }
         
         // Get or create parent collection for children.
         if (!entityParents.TryGetValue(childId, out var parents))
         {
            parents = new HashSet<int>();
            
            entityParents.Add(childId, parents);
         }
         
         // Do the actual pairing and invoke events.
         parents.Add(parentId);
         children.Add(childId);
         
         madeParentOfEvents.Publish(parentId, e => e.Invoke(parentId, childId));
         madeChildOfEvents.Publish(childId, e => e.Invoke(parentId, childId));
      }

      public void Unpair(int parentId, int childId)
      {
         AssertAlive(parentId);
         AssertAlive(childId);
         
         // Make sure parent has accepted children in the past.
         if (!entityChildren.TryGetValue(parentId, out var children))
            return;
         
         // Make sure children has accepted parent in the past.
         if (!entityParents.TryGetValue(childId, out var parents))
            return;
         
         // Do the actual unpairing and invoke events.
         children.Remove(childId);
         parents.Remove(parentId);
         
         unpairedFromChildEvents.Publish(parentId, e => e.Invoke(parentId, childId));
         unpairedFromParentEvents.Publish(childId, e => e.Invoke(parentId, childId));
      }

      public IEnumerable<int> ParentsOf(int childId)
      {  
         AssertAlive(childId);
         
         return entityParents.TryGetValue(childId, out var parents) ? parents : Enumerable.Empty<int>();
      }

      public IEnumerable<int> ChildrenOf(int parentId)
      {
         AssertAlive(parentId);
         
         return entityChildren.TryGetValue(parentId, out var children) ? children : Enumerable.Empty<int>();
      }

      public void Clear()
      {
         foreach (var id in aliveEntities.ToList())
            Delete(id);
      }

      public override void Initialize(IGameEngine engine)
      {
         base.Initialize(engine);

         // Create event queues.
         var events = Engine.Systems.First<IEventQueueSystem>();
         
         deletedEvents = events.CreateShared<int, EntityEventHandler>(EventQueueUsageHint.Lazy);
         
         unpairedFromChildEvents  = events.CreateShared<int, EntityPairEventHandler>(EventQueueUsageHint.Lazy);
         unpairedFromParentEvents = events.CreateShared<int, EntityPairEventHandler>(EventQueueUsageHint.Lazy);
         
         madeParentOfEvents = events.CreateShared<int, EntityPairEventHandler>(EventQueueUsageHint.Lazy);
         madeChildOfEvents  = events.CreateShared<int, EntityPairEventHandler>(EventQueueUsageHint.Lazy);
      }

      public IEnumerator<int> GetEnumerator()
         => aliveEntities.GetEnumerator();

      IEnumerator IEnumerable.GetEnumerator()
         => GetEnumerator();
   }
}