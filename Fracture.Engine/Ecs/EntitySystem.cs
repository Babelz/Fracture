using System;
using System.Collections;
using System.Collections.Generic;
using Fracture.Common.Collections;
using Fracture.Common.Di.Attributes;
using Fracture.Common.Events;
using Fracture.Engine.Core;
using Fracture.Engine.Events;

namespace Fracture.Engine.Ecs
{
   public readonly struct EntityEventArgs
   {
      #region Properties
      public int Id
      {
         get;
      }
      #endregion

      public EntityEventArgs(int id)
      {
         Id = id;
      }
   }
   
   public readonly struct EntityPairEventArgs
   {
      #region Properties
      public int ParentId
      {
         get;
      }

      public int ChildId
      {
         get;
      }
      #endregion

      public EntityPairEventArgs(int parentId, int childId)
      {
         ParentId = parentId;
         ChildId  = childId;
      }
   }

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
      IEvent<int, EntityEventArgs> Deleted
      {
         get;
      }
      
      /// <summary>
      /// Event invoked when entity has been unpaired from one of its parents.
      /// </summary>
      IEvent<int, EntityPairEventArgs> UnpairedFromChild
      {
         get;
      }
      
      /// <summary>
      /// Event invoked entity has been unpaired from its parent.
      /// </summary>
      IEvent<int, EntityPairEventArgs> UnpairedFromParent
      {
         get;
      }
      
      /// <summary>
      /// Event invoked when entity is made parent of other entity.
      /// </summary>
      IEvent<int, EntityPairEventArgs> MadeParentOf
      {
         get;
      }
      
      /// <summary>
      /// Event invoked when entity is made child of other entity.
      /// </summary>
      IEvent<int, EntityPairEventArgs> MadeChildOf
      {
         get;
      }
      #endregion
      
      /// <summary>
      /// Creates new entity with optional parameters.
      /// </summary>
      /// <returns>id of the new entity</returns>
      int Create(int? parentId = null, int? remoteId = null, int? annotation = null, string tag = "");
      /// <summary>
      /// Attempts to delete entity with given id. 
      /// </summary>
      void Delete(int id);
      
      /// <summary>
      /// Returns boolean declaring whether entity with given id
      /// is alive.
      /// </summary>
      bool Alive(int id);
      
      bool IsAnnotated(int id);
      int GetAnnotation(int id);
      
      string GetTag(int id);

      void Pair(int parentId, int childId);
      void Unpair(int parentId, int childId);
      
      bool HasParent(int id);
      int ParentOf(int id);
      
      bool LocalExists(int remoteId);
      int GetLocalId(int remoteId);
      
      IEnumerable<int> ChildrenOf(int parentId);
      
      void Clear();
   }

   /// <summary>
   /// Default implementation of <see cref="IEntitySystem"/>.
   /// </summary>
   public sealed class EntitySystem : GameEngineSystem, IEntitySystem
   {
      #region Entity structure
      private struct Entity
      {
         #region Properties
         public int Id
         {
            get;
            set;
         }
         public int SystemId
         {
            get;
            set;
         }
         public int? RemoteId
         {
            get;
            set;
         }
         public int? ParentId
         {
            get;
            set;
         }
         public List<int> ChildrenIds
         {
            get;
            set;
         }
         
         public int? Annotation
         {
            get;
            set;
         }
         public string Tag
         {
            get;
            set;
         }
         public bool Alive
         {
            get;
            set;
         }
         #endregion
      }
      #endregion

      #region Static fields
      private static int IdCounter;
      #endregion
      
      #region Fields
      private readonly int systemId;
      
      // Free (dead/unused) entities and alive entities. Get id from the free list to create new entity. This is required as some internal structures are 
      // linear and grow with the entity count.
      private readonly FreeList<int> freeEntityIds;
      private readonly HashSet<int> aliveEntityIds;

      private readonly Dictionary<int, int> remoteEntityIdMap;
      
      private readonly LinearGrowthArray<Entity> entities;

      // Entity events. 
      private readonly IEventQueue<int, EntityEventArgs> deletedEvents;
      private readonly IEventQueue<int, EntityPairEventArgs> unpairedFromChildEvents;
      private readonly IEventQueue<int, EntityPairEventArgs> unpairedFromParentEvents;
      private readonly IEventQueue<int, EntityPairEventArgs> madeParentOfEvents;
      private readonly IEventQueue<int, EntityPairEventArgs> madeChildOfEvents;
      #endregion

      #region Properties
      public IEvent<int, EntityEventArgs> Deleted
         => deletedEvents;
      
      public IEvent<int, EntityPairEventArgs> UnpairedFromChild
         => unpairedFromChildEvents;
      
      public IEvent<int, EntityPairEventArgs> UnpairedFromParent
         => unpairedFromParentEvents;
      
      public IEvent<int, EntityPairEventArgs> MadeParentOf
         => madeParentOfEvents;
      
      public IEvent<int, EntityPairEventArgs> MadeChildOf
         => madeChildOfEvents;
      #endregion

      [BindingConstructor]
      public EntitySystem(IEventQueueSystem events)
      {
         systemId = IdCounter++;
         
         deletedEvents = events.CreateShared<int, EntityEventArgs>();
         
         unpairedFromChildEvents  = events.CreateShared<int, EntityPairEventArgs>();
         unpairedFromParentEvents = events.CreateShared<int, EntityPairEventArgs>();
         
         madeParentOfEvents = events.CreateShared<int, EntityPairEventArgs>();
         madeChildOfEvents  = events.CreateShared<int, EntityPairEventArgs>();

         // Create entity data. 
         var idc = 0;
         
         freeEntityIds     = new FreeList<int>(() => idc++);
         aliveEntityIds    = new HashSet<int>();
         entities          = new LinearGrowthArray<Entity>(128);
         remoteEntityIdMap = new Dictionary<int, int>();
      }
      
      private void AssertAlive(int id)
      {
         if (!Alive(id)) 
            throw new InvalidOperationException($"entity {id} does not exist");
      }
      
      public int Create(int? parentId = null, int? remoteId = null, int? annotation = null, string tag = "")
      {
         var id = freeEntityIds.Take();
         
         while (id >= entities.Length)
            entities.Grow();
         
         ref var entity = ref entities.AtIndex(id);
         
         entity.Id            = id;
         entity.SystemId      = systemId;
         entity.RemoteId      = remoteId;
         entity.Annotation    = annotation;
         entity.Tag           = tag;
         entity.Alive         = true;
         entity.ChildrenIds ??= new List<int>();

         if (remoteId.HasValue)
            remoteEntityIdMap.Add(remoteId.Value, id);
         
         // Create events.
         deletedEvents.Create(id);
         unpairedFromChildEvents.Create(id);
         unpairedFromParentEvents.Create(id);
         madeParentOfEvents.Create(id);
         madeChildOfEvents.Create(id);
         
         aliveEntityIds.Add(id);
         
         // Lastly pair.
         if (parentId.HasValue)
            Pair(parentId.Value, id);
         
         return id;
      }

      public void Delete(int id)
      {
         AssertAlive(id);
         
         ref var entity = ref entities.AtIndex(id);
         
         // Publish deleted event.
         deletedEvents.Publish(id, new EntityEventArgs(id));

         // Delete and unpair children.
         foreach (var childId in entity.ChildrenIds)
         {
            Unpair(id, childId);
            
            Delete(childId);
         }
         
         // Unpair from parents.
         if (entity.ParentId.HasValue)
            Unpair(entity.ParentId.Value, id);
         
         // Remove from remote and annotation lookups.
         if (entity.RemoteId.HasValue)
            remoteEntityIdMap.Remove(entity.RemoteId.Value);
         
         // Delete all events.
         deletedEvents.Delete(id);
         unpairedFromChildEvents.Delete(id);
         unpairedFromParentEvents.Delete(id);
         madeParentOfEvents.Delete(id);
         madeChildOfEvents.Delete(id);
         
         // Clear rest of the state and return id to pool.
         freeEntityIds.Return(id);
       
         // Mark as not alive.
         entity.Alive = false;
         
         aliveEntityIds.Remove(id);
      }

      public bool Alive(int id)
         => id < entities.Length && entities.AtIndex(id).Alive;

      public bool IsAnnotated(int id)
      {
         AssertAlive(id);
         
         return entities.AtIndex(id).Annotation.HasValue;
      }

      public int GetAnnotation(int id)
      {
         AssertAlive(id);
         
         return entities.AtIndex(id).Annotation!.Value;
      }

      public string GetTag(int id)
      {
         AssertAlive(id);
         
         return entities.AtIndex(id).Tag;
      }

      public void Pair(int parentId, int childId)
      {
         if (parentId == childId)
            throw new InvalidOperationException($"can't pair same entity {parentId}");
         
         AssertAlive(parentId);
         AssertAlive(childId);
         
         ref var parent = ref entities.AtIndex(parentId);
         ref var child  = ref entities.AtIndex(childId);
         
         // Unpair from old parent.
         if (child.ParentId.HasValue)
            Unpair(child.ParentId.Value, child.Id);
         
         // Make child of new parent.
         parent.ChildrenIds.Add(childId);
         
         // Pair with new parent.
         child.ParentId = parentId;
         
         madeParentOfEvents.Publish(parentId, new EntityPairEventArgs(parentId, childId));
         madeChildOfEvents.Publish(childId, new EntityPairEventArgs(parentId, childId));
      }

      public void Unpair(int parentId, int childId)
      {
         AssertAlive(parentId);
         AssertAlive(childId);
         
         ref var parent = ref entities.AtIndex(parentId);
         ref var child  = ref entities.AtIndex(childId);

         // Unpair parent from children.
         parent.ChildrenIds.Remove(childId);
         
         // Unpair child from parent.
         child.ParentId = null;

         unpairedFromChildEvents.Publish(parentId, new EntityPairEventArgs(parentId, childId));
         unpairedFromParentEvents.Publish(childId, new EntityPairEventArgs(parentId, childId));
      }
      
      public bool HasParent(int id)
      {
         AssertAlive(id);
         
         return entities.AtIndex(id).ParentId.HasValue;
      }
      
      public int ParentOf(int id)
      {  
         AssertAlive(id);
         
         return entities.AtIndex(id).ParentId!.Value;
      }
      
      public bool LocalExists(int remoteId)
         => remoteEntityIdMap.ContainsKey(remoteId);
      
      public int GetLocalId(int remoteId)
         => remoteEntityIdMap[remoteId];

      public IEnumerable<int> ChildrenOf(int parentId)
      {
         AssertAlive(parentId);
         
         return entities.AtIndex(parentId).ChildrenIds;
      }

      public void Clear()
      {
         foreach (var id in aliveEntityIds)
            Delete(id);
      }

      public IEnumerator<int> GetEnumerator()
         => aliveEntityIds.GetEnumerator();

      IEnumerator IEnumerable.GetEnumerator()
         => GetEnumerator();
   }
}