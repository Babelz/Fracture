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
    /// Static utility class containing entity prefab types and utility functions.
    /// </summary>
    public static class EntityPrefab
    {
        /// <summary>
        /// Attribute that denotes class to be a prefab container. To create a container create new class, have public constructor that is annotated with
        /// <see cref="BindingConstructorAttribute"/> and use it to capture any dependencies required for entity activation.
        /// </summary>
        [AttributeUsage(AttributeTargets.Class)]
        public sealed class ContainerAttribute : Attribute
        {
            public ContainerAttribute()
            {
            }
        }
        
        /// <summary>
        /// Attribute that denotes class method to be a prefab activator. One container can have multiple activators.
        /// </summary>
        [AttributeUsage(AttributeTargets.Method)]
        public sealed class ActivatorAttribute : Attribute
        {
            public ActivatorAttribute()
            {
            }
        }
    }

    /// <summary>
    /// Interface for implementing prefab systems. Prefab system loads and holds all prefabs.
    /// </summary>
    public interface IEntityPrefabSystem : IGameEngineSystem
    {
        /// <summary>
        /// Attempts to register prefab container to the system. Throws if the container is invalid.
        /// </summary>
        void Register<T>();
        
        /// <summary>
        /// Attempts to get prefab container of specific type. Throws if no prefab is found.
        /// </summary>
        T Get<T>();
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
            
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsPrefabContainer(MemberInfo info)
            => info.GetCustomAttribute<EntityPrefab.ContainerAttribute>() != null;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsValidatePrefabContainer(Type type)
        {
            if (type.GetMethods().Any(m => m.GetCustomAttribute<EntityPrefab.ActivatorAttribute>() != null))
                return true;     
            
            Log.Warn($"type {type.FullName} is annotated with {nameof(EntityPrefab.ContainerAttribute)} but it does not have any methods annotated with " +
                     $"{nameof(EntityPrefab.ActivatorAttribute)} attribute");
            
            return false;
        }
        
        public override void Initialize()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (!IsPrefabContainer(type))
                            continue;
                        
                        if (!IsValidatePrefabContainer(type))
                            continue;
                        
                        registry.Add(type, activator.Activate(type));
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e, "unhandled error occurred while loading entity prefabs");
                }
            }
        }

        public void Register<T>()
        {
            if (!IsPrefabContainer(typeof(T)))
                throw new ArgumentException($"type {typeof(T).FullName} is not a prefab container");
            
            if (!IsValidatePrefabContainer(typeof(T)))
                throw new ArgumentNullException($"container {typeof(T).FullName} does not have any activator methods");
            
            registry.Add(typeof(T), activator.Activate(typeof(T)));
        }

        public T Get<T>()
            => (T)registry[typeof(T)];
    }
}