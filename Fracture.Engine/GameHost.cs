using System;
using Fracture.Common.Di;
using Fracture.Common.Di.Binding;
using Fracture.Engine.Core;
using Fracture.Engine.Core.Systems;
using Fracture.Engine.Ecs;
using Fracture.Engine.Events;
using Fracture.Engine.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NLog;

namespace Fracture.Engine
{
    /// <summary>
    /// Abstract base class for implementing game hosts. In place of <see cref="Game"/> inherit from this class to create new game instance.
    /// </summary>
    public abstract class GameHost : IGameHost, IDisposable
    {
        private delegate void InitializeCallback();

        private delegate void UpdateCallback(IGameEngineTime time);

        #region Private game implementation
        private sealed class Game : Microsoft.Xna.Framework.Game
        {
            #region Fields
            private readonly UpdateCallback update;

            private readonly InitializeCallback initialize;
            #endregion

            #region Properties
            public GraphicsDeviceManager GraphicsDeviceManager
            {
                get;
            }

            public GameEngineTime Time
            {
                get;
            }
            #endregion

            public Game(UpdateCallback update, InitializeCallback initialize)
            {
                this.update     = update ?? throw new ArgumentNullException(nameof(update));
                this.initialize = initialize ?? throw new ArgumentNullException(nameof(initialize));

                Time = new GameEngineTime();

                GraphicsDeviceManager = new GraphicsDeviceManager(this)
                {
                    GraphicsProfile = GraphicsProfile.HiDef,
                };
            }

            protected override void Update(GameTime gameTime)
            {
                base.Update(gameTime);

                Time.Elapsed = gameTime.ElapsedGameTime;
                Time.Total   = gameTime.TotalGameTime;

                update(Time);

                Time.Tick++;
            }

            protected override void Initialize()
            {
                base.Initialize();

                Content.RootDirectory = "Content";

                initialize();
            }
        }
        #endregion

        #region Static fields
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion

        #region Events
        public event EventHandler Exiting;
        #endregion

        #region Fields
        private readonly Game game;

        private readonly GameEngineSystemHost systems;
        private readonly Kernel               kernel;
        #endregion

        #region Properties
        public string[] Args
        {
            get;
        }

        public bool IsFixedTimeStep
        {
            get => game.IsFixedTimeStep;
            set => game.IsFixedTimeStep = value;
        }

        public TimeSpan TargetElapsedTime
        {
            get => game.TargetElapsedTime;
            set => game.TargetElapsedTime = value;
        }
        #endregion

        protected GameHost(string[] args)
        {
            Args = args;

            kernel  = new Kernel(DependencyBindingOptions.Interfaces | DependencyBindingOptions.BaseType);
            systems = new GameEngineSystemHost(kernel);
            game    = new Game(Update, Initialize);

            game.Exiting += Game_OnExiting;

            kernel.Bind(this);
        }

        #region Event handlers
        private void Game_OnExiting(object sender, EventArgs e)
        {
            Exiting?.Invoke(this, EventArgs.Empty);

            foreach (var system in systems.GetInOrder())
                system.Deinitialize();
        }
        #endregion

        private void Update(IGameEngineTime time)
        {
            foreach (var system in systems.GetInOrder())
                system.Update(time);
        }

        private void Initialize()
        {
            // Bind core systems that every game should have. 
            Log.Info($"binding core systems...");

            systems.Bind(new GameTimeSystem(game.Time));

            systems.Bind(new GraphicsDeviceSystem(game.GraphicsDeviceManager, game.Window));
            systems.Bind(new ContentSystem(game.Content));
            systems.Bind(new GameObjectActivatorSystem(kernel));

            systems.Bind<EventQueueSystem>();
            systems.Bind<EventSchedulerSystem>();

            systems.Bind<EntitySystem>();
            systems.Bind<EntityPrefabSystem>();

            // Allow game to bind game specific bindings.
            Log.Info($"binding game specific systems...");

            Initialize(systems);

            // Initialize all systems bound to the kernel.
            Log.Info("initializing systems...");

            foreach (var system in systems.GetInOrder())
            {
                Log.Info($"initializing system {system.GetType().Name}...");

                system.Initialize();
            }

            Start(systems);
        }

        /// <summary>
        /// Override in inheriting game to initialize all your game systems and perform any configuration require before starting the game. You should not
        /// touch systems at this point as they are not yet initialized.
        /// </summary>
        protected virtual void Initialize(IGameEngineSystemBinder binder)
        {
        }

        /// <summary>
        /// Override in inheriting game to start the game. Run any post initialization configurations and start running the game logic.
        /// </summary>
        protected virtual void Start(IGameEngineSystemHost systems)
        {
        }

        public void Run()
            => game.Run();

        public void Exit()
            => game.Exit();

        public void Dispose()
            => game.Dispose();
    }
}