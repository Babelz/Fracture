using System;
using System.Collections.Generic;

namespace Fracture.Engine.Core.Systems
{
    /// <summary>
    /// Abstract base class for implementing scenes. Scenes
    /// are responsible of initialization and cleanup of game logic. 
    /// </summary>
    public abstract class Scene
    {
        #region Properties
        protected IGameEngine Engine
        {
            get;
        }
        #endregion

        protected Scene(IGameEngine engine)
            => Engine = engine ?? throw new ArgumentNullException(nameof(engine));

        /// <summary>
        /// Initializes the scene, allowing it to allocate, locate
        /// and initialize resources it require if required.
        /// </summary>
        public virtual void Initialize(Scene last)
        {
        }

        /// <summary>
        /// Deinitialize the scene, allowing it to do cleanup required.
        /// </summary>
        public virtual void Deinitialize(Scene next)
        {
        }

        public virtual void Update(IGameEngineTime time)
        {
        }
    }
    
    public delegate void SceneInitializeCallback(Scene last);
    public delegate void SceneDeinitializeCallback(Scene next);
    public delegate void SceneUpdateCallback(IGameEngineTime time);
    
    public sealed class DelegateScene : Scene
    {
        #region Fields
        private readonly SceneInitializeCallback initialize;

        private readonly SceneDeinitializeCallback deinitialize;
        private readonly SceneUpdateCallback update;
        #endregion

        public DelegateScene(IGameEngine engine, 
                             SceneInitializeCallback initialize, 
                             SceneDeinitializeCallback deinitialize,
                             SceneUpdateCallback update)
            : base(engine)
        {
            this.initialize   = initialize;
            this.deinitialize = deinitialize;
            this.update       = update;
        }

        public override void Initialize(Scene last)
            => initialize?.Invoke(last);

        public override void Deinitialize(Scene next)
            => deinitialize?.Invoke(next);

        public override void Update(IGameEngineTime time)
            => update?.Invoke(time);
    }
    
    public delegate void SceneChangedCallback(Scene last, Scene next);
    
    /// <summary>
    /// Interface for implementing scene systems. Scene systems are responsible
    /// of scene management. Scene systems contain all the scenes and can change between them
    /// at any time. Scenes should be implemented as volatile objects meaning that they lose
    /// their state when deinitialized.
    /// </summary>
    public interface ISceneSystem : IGameEngineSystem
    {
        #region Properties
        Scene Current
        {
            get;
        }
        
        Scene Last
        {
            get;
        }
        #endregion

        /// <summary>
        /// Adds new scene to the system for later use.
        /// </summary>
        void Push(Scene scene, SceneChangedCallback callback = null);

        /// <summary>
        /// Pop current active scene from the scene stack
        /// and initializes the next scene from the stack.
        /// </summary>
        Scene Pop(SceneChangedCallback callback = null);

        /// <summary>
        /// Pop scenes until predicate returns true.
        /// </summary>
        Scene PopUntil(Func<Scene, bool> predicate, SceneChangedCallback callback = null);
    }

    /// <summary>
    /// System responsible of scene management.
    /// </summary>
    public sealed class SceneSystem : ActiveGameEngineSystem, ISceneSystem
    {
        #region Fields
        private readonly Stack<Scene> scenes;
        #endregion
        
        #region Properties
        public Scene Current
        {
            get;
            private set;
        }

        public Scene Last
        {
            get;
            private set;
        }
        #endregion
        
        public SceneSystem()
            : base(0) => scenes = new Stack<Scene>();
        
        public void Push(Scene scene, SceneChangedCallback callback = null)
        {
            var last = scenes.Count == 0 ? null : scenes.Peek();
            
            last?.Deinitialize(scene);

            Last    = Current;
            Current = scene;

            scenes.Push(scene);

            scene.Initialize(last);

            callback?.Invoke(last, scene);

            Current = scene;
        }

        public Scene Pop(SceneChangedCallback callback = null)
        {
            if (scenes.Count == 0)
                throw new InvalidOperationException("no scenes in the stack");

            var last = scenes.Pop();
            var next = scenes.Count == 0 ? null : scenes.Peek();

            last.Deinitialize(next);
            
            Current = next;
            Last    = last;

            next?.Initialize(last);

            callback?.Invoke(last, next);

            Current = next;

            return last;
        }
        
        public Scene PopUntil(Func<Scene, bool> predicate, SceneChangedCallback callback = null)
        {
            var last = scenes.Pop();
            
            Last = last;

            while (scenes.Count > 0)
            {
                var next = scenes.Peek();

                if (!predicate(next))
                {
                    scenes.Pop();

                    Last = next;
                }
                else
                {
                    last.Deinitialize(next);

                    next.Initialize(last);

                    callback?.Invoke(last, next);

                    Current = next;

                    return Current;
                }
            }

            throw new InvalidOperationException("no scene matches predicate");
        }

        public override void Update(IGameEngineTime time)
            => Current?.Update(time);
    }
}