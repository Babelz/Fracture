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
        int OwnerOf(int componentId);

        /// <summary>
        /// Returns boolean declaring whether a component with given id is alive.
        /// </summary>
        bool IsAlive(int componentId);

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
        bool Delete(int componentId);
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

        private readonly IEventHandler<int, EntityEventArgs> entityDeletedEvents;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the ids of alive components.
        /// </summary>
        protected IEnumerable<int> Alive => aliveComponents;
        #endregion

        protected ComponentSystem(IEventQueueSystem events)
        {
            entityDeletedEvents = events.GetEventHandler<int, EntityEventArgs>();

            // Create basic component data.
            var idc = 0;

            freeComponents       = new FreeList<int>(() => idc++);
            aliveComponents      = new List<int>();
            componentToEntityMap = new Dictionary<int, int>();
        }

        protected void AssertAlive(int componentId)
        {
#if DEBUG || ECS_RUNTIME_CHECKS
            if (!IsAlive(componentId))
                throw new InvalidOperationException($"component {componentId} does not exist");
#endif
        }

        /// <summary>
        /// Gets the id of component at given index.
        /// </summary>
        protected int IdAtIndex(int index)
            => aliveComponents[index];

        protected int PeekNextComponentId()
            => freeComponents.Peek();
        
        protected virtual int InitializeComponent(int entityId)
        {
            var componentId = freeComponents.Take();

            // Make sure this component is not duplicated.
            if (componentToEntityMap.ContainsKey(componentId))
                throw new InvalidOperationException($"duplicated internal component id {componentId}");

            aliveComponents.Add(componentId);
            componentToEntityMap.Add(componentId, entityId);

            return componentId;
        }

        public int OwnerOf(int componentId)
            => componentToEntityMap[componentId];

        public virtual bool Delete(int componentId)
        {
            if (!componentToEntityMap.Remove(componentId))
                return false;

            aliveComponents.Remove(componentId);
            componentToEntityMap.Remove(componentId);

            freeComponents.Return(componentId);

            return true;
        }

        public bool IsAlive(int componentId)
            => componentToEntityMap.ContainsKey(componentId);

        public abstract bool BoundTo(int entityId);
        public abstract int FirstFor(int entityId);
        public abstract IEnumerable<int> AllFor(int entityId);

        public override void Update(IGameEngineTime time)
            => entityDeletedEvents.Handle((in Letter<int, EntityEventArgs> letter) =>
            {
                foreach (var componentId in AllFor(letter.Args.EntityId))
                    Delete(componentId);

                return LetterHandlingResult.Retain;
            });

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
            : base(events)
            => entityToComponentMap = new Dictionary<int, int>();

        protected override int InitializeComponent(int entityId)
        {
            if (BoundTo(entityId))
                throw new InvalidOperationException($"entity {entityId} already has unique component");

            var componentId = base.InitializeComponent(entityId);

            entityToComponentMap.Add(entityId, componentId);

            return componentId;
        }

        public override bool Delete(int componentId)
        {
            var success = base.Delete(componentId);

            if (success)
                entityToComponentMap.Remove(componentId);

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
            => EntityComponentListPool = new CollectionPool<List<int>>(
                new Pool<List<int>>(new LinearStorageObject<List<int>>(new LinearGrowthArray<List<int>>()))
            );

        protected SharedComponentSystem(IEventQueueSystem events)
            : base(events)
            => entityToComponentsMap = new Dictionary<int, List<int>>();

        protected override int InitializeComponent(int entityId)
        {
            var componentId = base.InitializeComponent(entityId);

            if (!entityToComponentsMap.TryGetValue(entityId, out var componentList))
            {
                componentList = EntityComponentListPool.Take();

                entityToComponentsMap.Add(entityId, componentList);
            }

            componentList.Add(componentId);

            return componentId;
        }

        public override bool Delete(int componentId)
        {
            if (!IsAlive(componentId))
                return false;

            var entityId = OwnerOf(componentId);

            if (!base.Delete(componentId))
                return false;

            // Remove from map.
            var components = entityToComponentsMap[entityId];

            components.Remove(componentId);

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