using System;
using System.Collections.Generic;
using System.Linq;
using Fracture.Common.Di;
using Fracture.Common.Di.Binding;
using Fracture.Engine.Core;
using NLog;

namespace Fracture.Engine
{
    public interface IGameEngineSystemBinder
    {
        void Bind<T>(params IBindingValue [] bindings) where T : IGameEngineSystem;
        void Bind<T>(T system) where T : IGameEngineSystem;
    }

    public interface IGameEngineSystemHost : IGameEngineSystemBinder
    {
        IEnumerable<T> All<T>() where T : IGameEngineSystem;
        T First<T>() where T : IGameEngineSystem;

        void Verify();
    }

    public sealed class GameEngineSystemHost : IGameEngineSystemHost
    {
        #region Static fields
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion

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

        public void Bind<T>(params IBindingValue [] bindings) where T : IGameEngineSystem
        {
            Log.Info($"binding system {typeof(T).FullName}...");

            context.Add(count++, typeof(T));

            kernel.Bind<T>(bindings);
        }

        public void Bind<T>(T system) where T : IGameEngineSystem
        {
            Log.Info($"binding system {typeof(T).FullName}...");

            context.Add(count++, system.GetType());

            kernel.Bind(system);
        }

        public IEnumerable<T> All<T>() where T : IGameEngineSystem
            => kernel.All<T>();

        public T First<T>() where T : IGameEngineSystem
            => kernel.First<T>();

        public IEnumerable<IGameEngineSystem> GetInOrder()
            => context.Select(c => kernel.First(c.Value)).Cast<IGameEngineSystem>();

        public void Verify()
            => kernel.Verify();
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
        string [] Args
        {
            get;
        }

        bool IsFixedTimeStep
        {
            get;
            set;
        }

        TimeSpan TargetElapsedTime
        {
            get;
            set;
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