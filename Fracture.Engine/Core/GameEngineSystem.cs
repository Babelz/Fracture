using System;

namespace Fracture.Engine.Core
{
    /// <summary>
    /// Interface for implementing game engine systems. 
    /// </summary>
    public interface IGameEngineSystem
    {
        /// <summary>
        /// Allows the system to do initialization.
        /// </summary>
        void Initialize(IGameEngine engine);

        /// <summary>
        /// Allows the system do deinitialization.
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
        /// Allows the system to do updates.
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
            private set;
        }
        #endregion

        /// <summary>
        /// Creates new instance of <see cref="GameEngineSystem"/>. Use
        /// constructors to setup any configuration for the system. Use the
        /// initialize method to do any initialization for your system. Systems
        /// can setup their dependencies in three ways:
        ///     1) using binding properties, binding happens before calling initialize
        ///     2) using binding method, calling this happens before calling initialize
        ///     3) using the initialize method and manually locating all dependencies
        ///
        /// Option 3 should be preferred when:
        ///     - system locates services
        ///     - system locates only 1 dependency
        ///     - system needs to do some special initialization when it locates the services
        ///     - the system does not need the dependency after initialization step
        ///
        /// Options 3 should be preferred because of:
        ///     - it makes locating dependencies easy for the developer
        ///     - it is clear how dependencies are located
        ///     - it makes clear when dependencies are located
        ///     - it just fucking works you monkey
        /// 
        /// 1 and 2 should be used when no service location is required or option 3 rules are not violated.
        /// </summary>
        protected GameEngineSystem()
        {
        }

        public virtual void Deinitialize()
        {
        }
        
        // To bind dependencies using binding method, for example:
        //
        // [BindingMethod]
        // void Bind(ISystemA a, ISystemB b, ...) 
        //
        // Doing this you need to catch dependencies using.
        
        // To bind dependencies using binding properties, for example:
        //
        // [BindingProperty]
        // public ISystemA A { get; private set; }
        // OR
        // private ISystem A { get; set; }
        //
        // Binder will bind dependency here, properties can be public or private. 
        // Setter can be public or private.
        
        public virtual void Initialize(IGameEngine engine)
            => Engine = engine ?? throw new ArgumentNullException(nameof(engine));
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
        
        protected ActiveGameEngineSystem(int priority) 
        {
            Priority = priority;
        }
        
        public abstract void Update(IGameEngineTime time);
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
