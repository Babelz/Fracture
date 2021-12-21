using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fracture.Common.Di;
using Fracture.Common.Di.Binding;
using Fracture.Engine.Core;

namespace Fracture.Engine
{
    public interface IGameEngineSystemHost
    {
        void Bind<T>(params IBindingValue[] bindings) where T : IGameEngineSystem;
        void Bind<T>(T system) where T : IGameEngineSystem;
        
        IEnumerable<T> All<T>() where T : IGameEngineSystem;
        T First<T>() where T : IGameEngineSystem;
    }

    public sealed class GameEngineSystemHost : IGameEngineSystemHost
    {
        #region Fields
        private readonly SortedList<int, Type> context;
        
        private readonly Kernel kernel;
        
        private int count;
        #endregion

        public GameEngineSystemHost(Kernel kernel)
        {
            this.kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
            
            context = new SortedList<int, Type>();
        }
        
        public void Bind<T>(params IBindingValue[] bindings) where T : IGameEngineSystem
        {
            context.Add(count++, typeof(T));
            
            kernel.Bind<T>(bindings);
        }

        public void Bind<T>(T system) where T : IGameEngineSystem
        {
            context.Add(count++, system.GetType());
            
            kernel.Bind(system);
        }

        public IEnumerable<T> All<T>() where T : IGameEngineSystem
            => kernel.All<T>();

        public T First<T>() where T : IGameEngineSystem
            => kernel.First<T>();
        
        public IEnumerable<IGameEngineSystem> GetInOrder()
            => context.Select(c => kernel.First(c.Value)).Cast<IGameEngineSystem>();
    }

    /// <summary>
    /// Interface that provides operations for communicating with the currently running game.
    /// </summary>
    public interface IGameHost
    {
        #region Properties
        /// <summary>
        /// Gets the startup arguments passed to the game
        /// </summary>
        public string[] Args
        {
            get;
        }
        #endregion
        
        #region Events
        /// <summary>
        /// Event invoked when the game is exiting.
        /// </summary>
        event EventHandler Exiting;
        #endregion
        
        /// <summary>
        /// Signals th game to exit.
        /// </summary>
        void Exit();
    }
}
