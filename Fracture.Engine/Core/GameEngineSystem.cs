using System;
using Fracture.Common.Di.Attributes;

namespace Fracture.Engine.Core
{
    /// <summary>
    /// Interface for implementing game engine systems. 
    /// </summary>
    public interface IGameEngineSystem
    {
        /// <summary>
        /// Allows the system to do initialization. This is called for each system before the engine start to run the game loop.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Allows the system do deinitialization. This is called for each system before the game loop exists.
        /// </summary>
        void Deinitialize();
    }
    
    /// <summary>
    /// Game engine system with active back end meaning it
    /// requires updates every frame.
    /// </summary>
    public interface IActiveGameEngineSystem : IGameEngineSystem
    {
        #region Properties
        int Priority
        {
            get;
        }
        #endregion
        
        /// <summary>
        /// Allows the system to do updates. This is called for each system on every frame.
        /// </summary>
        void Update();
    }
    
    /// <summary>
    /// Game engine system that reacts to events.
    /// </summary>
    /// <typeparam name="T">even this system reacts to</typeparam>
    public interface IReactiveGameEngineSystem<in T> : IGameEngineSystem
    {
        void React(T notification);
    }
    
    /// <summary>
    /// System that works as game engine system and is managing
    /// objects of specific type. Systems should own the objects and
    /// manage their life span.
    ///
    /// Create more specific methods for managing the objects if
    /// suited. Use this interface to allow system to reset it's state
    /// to initial state.
    /// </summary>
    public interface IObjectManagementSystem : IGameEngineSystem
    {
        /// <summary>
        /// Clears all objects from the system and releases all resources
        /// owned by them. Caller should assume that any disposable objects
        /// are disposed after this and are unsafe to use after.
        /// </summary>
        void Clear();
    }

    /// <summary>
    /// Abstract base class for implementing game engine systems.
    /// </summary>
    public abstract class GameEngineSystem : IGameEngineSystem
    {
        #region Properties
        protected IGameEngine Engine
        {
            get;
        }
        #endregion

        /// <summary>
        /// Creates new instance of <see cref="GameEngineSystem"/>. Use
        /// constructors to setup any configuration for the system. Use the
        /// initialize method to do any initialization for your system. Setup
        /// system dependencies using the constructor by decorating it with <see cref="BindingConstructorAttribute"/>.
        /// </summary>
        protected GameEngineSystem(IGameEngine engine)
        {
            Engine = engine ?? throw new ArgumentNullException(nameof(engine));
        }

        public virtual void Deinitialize()
        {
        }
        
        public virtual void Initialize()
        {
        }
    }
 
    /// <summary>
    /// Static utility class containing priority hints
    /// that can ease the prioritization of systems. 
    /// </summary>
    public static class SystemPriorityHint
    {   
        // Keep gaps between priorities big to allow better
        // ordering of system updates.
        
        /// <summary>
        /// System updated at the beginning of the loop.
        /// </summary>
        public const int Beginning = 0;
        
        /// <summary>
        /// System updated at the middle of the loop.
        /// </summary>
        public const int Middle = 1000;
        
        /// <summary>
        /// System updated at the end of the loop.
        /// </summary>
        public const int Last = 2000;
    }
    
    /// <summary>
    /// Abstract base class for implementing active game engine systems.
    /// </summary>
    public abstract class ActiveGameEngineSystem : GameEngineSystem, IActiveGameEngineSystem
    {
        #region Properties
        public int Priority
        {
            get;
        }
        #endregion
        
        protected ActiveGameEngineSystem(IGameEngine engine, int priority)
            : base(engine)
        {
            Priority = priority;
        }
        
        public abstract void Update();
    }
    
    /// <summary>
    /// Abstract base class for implementing reactive game engine systems.
    /// </summary>
    public abstract class ReactiveGameEngineSystem<T> : GameEngineSystem, IReactiveGameEngineSystem<T>
    {
        protected ReactiveGameEngineSystem(IGameEngine engine)
            : base(engine)
        {
        }
        
        public abstract void React(T notification);
    }
}
