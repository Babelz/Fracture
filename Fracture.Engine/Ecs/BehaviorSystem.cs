using System;
using System.Collections.Generic;
using System.Linq;
using Fracture.Common.Collections;
using Fracture.Common.Di.Attributes;
using Fracture.Common.Di.Binding;
using Fracture.Common.Events;
using Fracture.Common.Memory.Pools;
using Fracture.Common.Memory.Storages;
using Fracture.Engine.Core;
using Fracture.Engine.Events;
using Fracture.Engine.Scripting;

namespace Fracture.Engine.Ecs
{
    public abstract class Behavior : ActiveCsScript
    {
        #region Events
        public event EventHandler Detaching;
        #endregion
        
        #region Properties
        /// <summary>
        /// Gets the id of the entity that this behaviour is attached to.
        /// </summary>
        public int EntityId
        {
            get;
            private set;
        }
        
        /// <summary>
        /// Gets boolean declaring whether the behaviour is attached.
        /// </summary>
        public bool Attached
        {
            get;
            private set;
        }
        #endregion
        
        /// <summary>
        /// Creates new instance of behaviour. Use <see cref="BindingConstructorAttribute"/> to parametrize behaviour activation.
        /// </summary>
        protected Behavior()
        {
        }
        
        /// <summary>
        /// Attaches the behaviour to given entity. When overriding call the base method.
        /// </summary>
        public virtual void Attach(int entityId)
        {
            if (Attached)
                throw new InvalidOperationException("already attached");
            
            EntityId = entityId;
            Attached = true;
        }
        
        /// <summary>
        /// Detaches the behaviour from the current entity. When overriding call the base method.
        /// </summary>
        public virtual void Detach()
        {
            if (!Attached)
                throw new InvalidOperationException("already detached");
            
            Attached = false;

            Detaching?.Invoke(this, EventArgs.Empty);
            
            EntityId = 0;
        }

        public override void Update(IGameEngineTime time)
        {
        }
    }

    /// <summary>
    /// Interface for implementing behaviour systems. Behaviours provide logical extension layer on top of ECS where behaviours contain the game logic
    /// and control entities and components. Behaviours can be controller by other systems or scrips and can communicate directly with each other.
    /// </summary>
    public interface IBehaviorSystem : IGameEngineSystem
    {
        /// <summary>
        /// Attaches behaviour of given type to given entity.
        /// </summary>
        void Attach<T>(int entityId, params IBindingValue[] bindings) where T : Behavior;
        
        /// <summary>
        /// Returns first behaviour of specified type to the caller that is attached to given entity. Throws exception if no behaviour exists. 
        /// </summary>
        T FirstOfType<T>(int entityId) where T : Behavior;

        /// <summary>
        /// Attempts to get first behaviour of specified type to the caller that is attached to given entity. Returns boolean whether the behaviour was found. 
        /// </summary>
        bool TryGetFirstOfType<T>(int entityId, out T behavior) where T : Behavior;

        /// <summary>
        /// Returns boolean declaring whether given entity has behaviour of given type attached to it.
        /// </summary>
        bool Attached<T>(int entityId) where T : Behavior;
        
        /// <summary>
        /// Gets all behaviours currently attached to given entity.
        /// </summary>
        IEnumerable<Behavior> BehaviorsOf(int entityId);
    }
    
    public sealed class BehaviorSystem : GameEngineSystem, IBehaviorSystem
    {
        #region Static fields
        private static readonly CollectionPool<List<Behavior>> ListPool = new CollectionPool<List<Behavior>>(
            new Pool<List<Behavior>>(
                new LinearStorageObject<List<Behavior>>(
                    new LinearGrowthArray<List<Behavior>>()))
            );
        #endregion
        
        #region Fields
        private readonly ICsScriptingSystem scripts;
        
        private readonly Dictionary<int, List<Behavior>> entityBehaviourLists;
        
        private readonly IEventHandler<int, EntityEventArgs> entityDeletedEvents;
        #endregion
        
        [BindingConstructor]
        public BehaviorSystem(ICsScriptingSystem scripts, IEventQueueSystem events)
        {
            this.scripts = scripts;
        
            entityDeletedEvents = events.GetEventHandler<int, EntityEventArgs>();
            
            entityBehaviourLists = new Dictionary<int, List<Behavior>>();
        }
        
        public void Attach<T>(int entityId, params IBindingValue[] bindings) where T : Behavior
        {
            var behaviour = scripts.Load<T>(bindings);
            
            if (!entityBehaviourLists.TryGetValue(entityId, out var behaviors))
            {
                behaviors = ListPool.Take();
                
                entityBehaviourLists.Add(entityId, behaviors);
            }
            
            behaviors.Add(behaviour);
            
            behaviour.Attach(entityId);
        }
        
        public T FirstOfType<T>(int entityId) where T : Behavior
            => (T)entityBehaviourLists[entityId].First(b => b is T);
        
        public bool TryGetFirstOfType<T>(int entityId, out T behavior) where T : Behavior
        {
            behavior = null;
            
            if (!entityBehaviourLists.ContainsKey(entityId))
                return false;
            
            behavior = (T)entityBehaviourLists[entityId].FirstOrDefault(b => b is T);
            
            return behavior != null;
        }

        public bool Attached<T>(int entityId) where T : Behavior
            => entityBehaviourLists.ContainsKey(entityId) && entityBehaviourLists[entityId].Any(b => b is T);
        
        public IEnumerable<Behavior> BehaviorsOf(int entityId)
            => entityBehaviourLists[entityId];

        public override void Update(IGameEngineTime time)
        {
            entityDeletedEvents.Handle((in Letter<int, EntityEventArgs> letter) =>
            {
                if (!entityBehaviourLists.TryGetValue(letter.Args.EntityId, out var behaviors))
                    return LetterHandlingResult.Retain;
                
                foreach (var behavior in behaviors.Where(behavior => behavior.Attached))
                    behavior.Detach();
                
                entityBehaviourLists.Remove(letter.Args.EntityId);
                ListPool.Return(behaviors);
                
                return LetterHandlingResult.Retain;
            });
        }
    }
}