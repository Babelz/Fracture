using System;
using System.Collections.Generic;
using System.Linq;
using Fracture.Common.Di.Attributes;
using Fracture.Common.Di.Binding;
using Fracture.Common.Memory;
using Fracture.Engine.Core;
using Fracture.Engine.Events;
using Fracture.Engine.Scripting;

namespace Fracture.Engine.Ecs
{
    public abstract class Behavior : ActiveCsScript
    {
        #region Properties
        public int Id
        {
            get;
        }

        public int EntityId
        {
            get;
        }
        #endregion
        
        public Behavior(int id, int entityId)
        {
            Id       = id;
            EntityId = entityId;
        }

        public override void Update(IGameEngineTime time)
        {
        }
    }

    public interface IBehaviorSystem : IComponentSystem
    {
        int Create<T>(int entityId, params IBindingValue[] bindings) where T : Behavior;
    }
    
    public sealed class BehaviorSystem : SharedComponentSystem, IBehaviorSystem
    {
        #region Fields
        private readonly ICsScriptingSystem scripts;

        private readonly Dictionary<int, HashSet<Behavior>> entityToBehaviourListMap;
        #endregion
        
        [BindingConstructor]
        public BehaviorSystem(IEntitySystem entities, IEventQueueSystem events, ICsScriptingSystem scripts) 
            : base(entities, events)
        {
            this.scripts = scripts ?? throw new ArgumentNullException(nameof(scripts));

            entityToBehaviourListMap = new Dictionary<int, HashSet<Behavior>>();
        }

        public int Create<T>(int entityId, params IBindingValue[] bindings) where T : Behavior
        {
            var id = InitializeComponent(entityId);
            
            var script = scripts.Load<T>(bindings.Concat(new[]
            {
                BindingValue.Const("entityId", entityId),
                BindingValue.Const("id", id)
            }).ToArray());

            if (!entityToBehaviourListMap.TryGetValue(entityId, out var behaviors))
            {
                behaviors = new HashSet<Behavior>();
                
                entityToBehaviourListMap.Add(entityId, behaviors);
            }

            script.Unloading += delegate
            {
                entityToBehaviourListMap[entityId].Remove(script);
            };
            
            void UnloadBehaviour(in EntityEventArgs args)
            {
                script.Unload();
                
                Entities.Deleted.Unsubscribe(entityId, UnloadBehaviour);
            }
            
            Entities.Deleted.Subscribe(entityId, UnloadBehaviour);
            
            return id;
        }
    }
}