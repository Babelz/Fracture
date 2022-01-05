using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Fracture.Common.Di;
using Fracture.Common.Di.Attributes;
using Fracture.Engine.Core;
using Fracture.Engine.Core.Systems;
using NLog;

namespace Fracture.Engine.Ecs
{
    /// <summary>
    /// Interface for implementing entity prefabs.
    /// </summary>
    public interface IEntityPrefab
    {
        // Nothing to implement, marker interface.
    }

    /// <summary>
    /// Interface for implementing prefab systems. Prefab system loads and holds all prefabs.
    /// </summary>
    public interface IEntityPrefabSystem : IGameEngineSystem
    {
        /// <summary>
        /// Attempts to register prefab container to the system. Throws if the container is invalid.
        /// </summary>
        void Register<T>() where T : IEntityPrefab;
        
        /// <summary>
        /// Attempts to get prefab container of specific type. Throws if no prefab is found.
        /// </summary>
        T Get<T>() where T : IEntityPrefab;
    }
    
    /// <summary>
    /// Default implementation of <see cref="IEntityPrefabSystem"/>.
    /// </summary>
    public sealed class EntityPrefabSystem : GameEngineSystem, IEntityPrefabSystem
    {
        #region Static fields
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion
        
        #region Fields
        private readonly IGameObjectActivatorSystem activator;
        
        private readonly Dictionary<Type, object> registry;
        #endregion
        
        [BindingConstructor]
        public EntityPrefabSystem(IGameObjectActivatorSystem activator)
        {
            this.activator = activator ?? throw new ArgumentNullException(nameof(activator));
            
            registry = new Dictionary<Type, object>();
        }

        public override void Initialize()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                        registry.Add(type, activator.Activate(type));
                }
                catch (Exception e)
                {
                    Log.Error(e, "unhandled error occurred while loading entity prefabs");
                }
            }
        }

        public void Register<T>() where T : IEntityPrefab
            => registry.Add(typeof(T), activator.Activate(typeof(T)));
        
        public T Get<T>() where T : IEntityPrefab
            => (T)registry[typeof(T)];
    }
}