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
        
        /// <summary>
        /// Allows the system to do updates. This is called for each system on every frame.
        /// </summary>
        void Update(IGameEngineTime time);
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
    /// suited. Use this interface to allow system to reset its state
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
        /// <summary>
        /// Creates new instance of <see cref="GameEngineSystem"/>. Use
        /// constructors to setup any configuration for the system. Use the
        /// initialize method to do any initialization for your system. Setup
        /// system dependencies using the constructor by decorating it with <see cref="BindingConstructorAttribute"/>.
        /// </summary>
        protected GameEngineSystem()
        {
        }

        public virtual void Deinitialize()
        {
        }
        
        public virtual void Initialize()
        {
        }

        public virtual void Update(IGameEngineTime time)
        {
        }
    }
    
    /// <summary>
    /// Abstract base class for implementing reactive game engine systems.
    /// </summary>
    public abstract class ReactiveGameEngineSystem<T> : GameEngineSystem, IReactiveGameEngineSystem<T>
    {
        protected ReactiveGameEngineSystem()
        {
        }
        
        public abstract void React(T notification);
    }
}
