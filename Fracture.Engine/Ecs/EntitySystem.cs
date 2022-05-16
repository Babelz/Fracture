using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        public int EntityId
        {
            get;
        }
        #endregion

        public EntityEventArgs(int entityId)
            => EntityId = entityId;
    }

    public enum EntityPairEventAction : byte
    {
        MadeParent = 0,
        MadeChild,
        ChildrenRemoved,
        ParentRemoved,
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

        public EntityPairEventAction Action
        {
            get;
        }
        #endregion

        public EntityPairEventArgs(int parentId, int childId, EntityPairEventAction action)
        {
            ParentId = parentId;
            ChildId  = childId;
            Action   = action;
        }
    }

    /// <summary>
    /// Interface for implementing entity systems. Entity in Minima is
    /// just a identifier and systems associated them with data and operations.
    /// </summary>
    public interface IEntitySystem : IGameEngineSystem, IEnumerable<int>
    {
        /// <summary>
        /// Creates new entity with optional parameters.
        /// </summary>
        /// <returns>id of the new entity</returns>
        int Create(int? parentId = null, int? remoteId = null, int? annotation = null, string tag = "");

        /// <summary>
        /// Attempts to delete entity with given id. 
        /// </summary>
        void Delete(int entityId);

        /// <summary>
        /// Returns boolean declaring whether entity with given id
        /// is alive.
        /// </summary>
        bool Alive(int entityId);

        bool IsAnnotated(int entityId);
        int GetAnnotation(int entityId);

        string GetTag(int entityId);

        void Pair(int parentId, int childId);
        void Unpair(int parentId, int childId);

        bool HasParent(int entityId);
        int ParentOf(int entityId);

        bool LocalExists(int remoteId);
        int LocalIdOf(int remoteId);

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

        #region Constant fields
        private const int EntitiesCapacity = 1024;
        #endregion

        #region Fields
        // Free (dead/unused) entities and alive entities. Get id from the free list to create new entity. This is required as some internal structures are 
        // linear and grow with the entity count.
        private readonly FreeList<int> freeEntityIds;
        private readonly HashSet<int>  aliveEntityIds;

        private readonly Dictionary<int, int> remoteEntityIdMap;

        private readonly LinearGrowthArray<Entity> entities;

        // Entity events. 
        private readonly IUniqueEvent<int, EntityEventArgs> deletedEvent;

        private readonly ISharedEvent<int, EntityPairEventArgs> pairEvent;
        #endregion

        [BindingConstructor]
        public EntitySystem(IEventQueueSystem events)
        {
            deletedEvent = events.CreateUnique<int, EntityEventArgs>();
            pairEvent    = events.CreateShared<int, EntityPairEventArgs>();

            // Create entity data. 
            var idc = 0;

            freeEntityIds     = new FreeList<int>(() => idc++);
            aliveEntityIds    = new HashSet<int>();
            entities          = new LinearGrowthArray<Entity>(EntitiesCapacity);
            remoteEntityIdMap = new Dictionary<int, int>();
        }

        private void AssertAlive(int entityId)
        {
            if (!Alive(entityId))
                throw new InvalidOperationException($"entity {entityId} does not exist");
        }

        public int Create(int? parentId = null, int? remoteId = null, int? annotation = null, string tag = "")
        {
            // Get next free entity id and reserve space for it.
            var entityId = freeEntityIds.Take();

            while (entityId >= entities.Length)
                entities.Grow();

            // Decorate entity structure.
            ref var entity = ref entities.AtIndex(entityId);

            entity.Id          =   entityId;
            entity.RemoteId    =   remoteId;
            entity.Annotation  =   annotation;
            entity.Tag         =   tag;
            entity.Alive       =   true;
            entity.ChildrenIds ??= new List<int>();

            if (remoteId.HasValue)
                remoteEntityIdMap.Add(remoteId.Value, entityId);

            // Create events.
            deletedEvent.Create(entityId);
            pairEvent.Create(entityId);

            aliveEntityIds.Add(entityId);

            // Lastly pair.
            if (parentId.HasValue)
                Pair(parentId.Value, entityId);

            return entityId;
        }

        public void Delete(int entityId)
        {
            AssertAlive(entityId);

            ref var entity = ref entities.AtIndex(entityId);

            // Publish deleted event.
            deletedEvent.Publish(entityId, new EntityEventArgs(entityId));

            // Delete and unpair children.
            foreach (var childId in entity.ChildrenIds)
            {
                Unpair(entityId, childId);

                Delete(childId);
            }

            // Unpair from parents.
            if (entity.ParentId.HasValue)
                Unpair(entity.ParentId.Value, entityId);

            // Remove from remote and annotation lookups.
            if (entity.RemoteId.HasValue)
                remoteEntityIdMap.Remove(entity.RemoteId.Value);

            // Delete all events.
            deletedEvent.Delete(entityId);

            pairEvent.Delete(entityId);

            // Clear rest of the state and return id to pool.
            freeEntityIds.Return(entityId);

            // Mark as not alive.
            entity.Alive = false;

            aliveEntityIds.Remove(entityId);
        }

        public bool Alive(int entityId)
            => entityId < entities.Length && entities.AtIndex(entityId).Alive;

        public bool IsAnnotated(int entityId)
        {
            AssertAlive(entityId);

            return entities.AtIndex(entityId).Annotation.HasValue;
        }

        public int GetAnnotation(int entityId)
        {
            AssertAlive(entityId);

            return entities.AtIndex(entityId).Annotation!.Value;
        }

        public string GetTag(int entityId)
        {
            AssertAlive(entityId);

            return entities.AtIndex(entityId).Tag;
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

            pairEvent.Publish(parentId, new EntityPairEventArgs(parentId, childId, EntityPairEventAction.MadeParent));
            pairEvent.Publish(parentId, new EntityPairEventArgs(childId, parentId, EntityPairEventAction.MadeChild));
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

            pairEvent.Publish(parentId, new EntityPairEventArgs(parentId, childId, EntityPairEventAction.ChildrenRemoved));
            pairEvent.Publish(parentId, new EntityPairEventArgs(childId, parentId, EntityPairEventAction.ParentRemoved));
        }

        public bool HasParent(int entityId)
        {
            AssertAlive(entityId);

            return entities.AtIndex(entityId).ParentId.HasValue;
        }

        public int ParentOf(int entityId)
        {
            AssertAlive(entityId);

            return entities.AtIndex(entityId).ParentId!.Value;
        }

        public bool LocalExists(int remoteId)
            => remoteEntityIdMap.ContainsKey(remoteId);

        public int LocalIdOf(int remoteId)
            => remoteEntityIdMap[remoteId];

        public IEnumerable<int> ChildrenOf(int parentId)
        {
            AssertAlive(parentId);

            return entities.AtIndex(parentId).ChildrenIds;
        }

        public void Clear()
        {
            foreach (var entityId in aliveEntityIds.ToList())
                Delete(entityId);
        }

        public IEnumerator<int> GetEnumerator()
            => aliveEntityIds.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}