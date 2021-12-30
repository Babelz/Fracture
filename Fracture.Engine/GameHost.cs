using System;
using System.Linq;
using Fracture.Common.Di;
using Fracture.Common.Di.Binding;
using Fracture.Engine.Core;
using Fracture.Engine.Core.Systems;
using Fracture.Engine.Ecs;
using Fracture.Engine.Graphics;
using Fracture.Engine.Input.Devices;
using Fracture.Engine.Scripting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
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
            private readonly GameEngineTime time;
            
            private readonly UpdateCallback update;

            private readonly InitializeCallback initialize;
            #endregion

            #region Properties
            public GraphicsDeviceManager GraphicsDeviceManager
            {
                get;
            }
            #endregion
            
            public Game(UpdateCallback update,
                        InitializeCallback initialize)
            {
                this.update     = update ?? throw new ArgumentNullException(nameof(update));
                this.initialize = initialize ?? throw new ArgumentNullException(nameof(initialize));

                time = new GameEngineTime();
                
                GraphicsDeviceManager = new GraphicsDeviceManager(this)
                {
                    GraphicsProfile = GraphicsProfile.HiDef
                };
            }

            protected override void Update(GameTime gameTime)
            {
                base.Update(gameTime);
                
                time.Elapsed = gameTime.ElapsedGameTime;
                time.Total   = gameTime.TotalGameTime;

                update(time);
                
                time.Frame++;
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
        private readonly Kernel kernel;
        #endregion

        #region Properties
        public string[] Args
        {
            get;
        }
        #endregion

        protected GameHost(string[] args)
        {
            Args = args;
            
            kernel  = new Kernel(DependencyBindingOptions.Interfaces | DependencyBindingOptions.Class);
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
            // Bind core systems that every game should have. Core systems can be distinguished from other systems based the fact that they do not have binding
            // constructors and they are always initialized with the engine.
            Log.Info($"binding core systems...");
            
            systems.Bind(new GraphicsDeviceSystem(game.GraphicsDeviceManager, game.Window));
            systems.Bind(new ContentSystem(game.Content));
            systems.Bind(new GameObjectActivatorSystem(kernel));

            // Allow game to bind game specific bindings.
            Log.Info($"binding game specific systems...");
            
            Initialize(systems);
            
            // Initialize all systems bound to the kernel.
            foreach (var system in systems.GetInOrder())
            {
                Log.Info($"initializing system {system.GetType().Name}...");
                
                system.Initialize();
            }
        }
        
        protected virtual void Initialize(IGameEngineSystemHost systems)
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