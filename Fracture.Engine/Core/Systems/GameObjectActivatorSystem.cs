using System;
using Fracture.Common.Di;
using Fracture.Common.Di.Attributes;
using Fracture.Common.Di.Binding;

namespace Fracture.Engine.Core.Systems
{
    /// <summary>
    /// Interface that provides object activation and dependency injection of engine systems to objects. This is a core system that is used to provide
    /// injection of game engine systems to components such as scripts.
    /// </summary>
    public interface IGameObjectActivatorSystem : IGameEngineSystem
    {
        /// <summary>
        /// Attempts to active given type using optional bindings. If object of type can't be activated and exception is thrown.
        /// </summary>
        T Activate<T>(params IBindingValue[] bindings);

        /// <summary>
        /// Attempts to active given type using optional bindings. If object of type can't be activated and exception is thrown.
        /// </summary>
        object Activate(Type type, params IBindingValue[] bindings);
    }

    /// <summary>
    /// Default implementation of <see cref="IGameObjectActivatorSystem"/>.
    /// </summary>
    public sealed class GameObjectActivatorSystem : GameEngineSystem, IGameObjectActivatorSystem
    {
        #region Fields
        private readonly IObjectActivator activator;
        #endregion

        [BindingConstructor]
        public GameObjectActivatorSystem(IObjectActivator activator)
            => this.activator = activator;

        public T Activate<T>(params IBindingValue[] bindings)
            => activator.Activate<T>(bindings);

        public object Activate(Type type, params IBindingValue[] bindings)
            => activator.Activate(type, bindings);
    }
}