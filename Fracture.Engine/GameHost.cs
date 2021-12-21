using System;
using System.Linq;
using Fracture.Common.Di;
using Fracture.Common.Di.Binding;
using Fracture.Engine.Core;
using Fracture.Engine.Core.Systems;
using Fracture.Engine.Graphics;
using Fracture.Engine.Input.Devices;
using Fracture.Engine.Scripting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

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

            private readonly InitializeCallback beforeInitialize;
            private readonly InitializeCallback initialize;
            private readonly InitializeCallback afterInitialize;
            #endregion

            #region Properties
            public GraphicsDeviceManager GraphicsDeviceManager
            {
                get;
            }
            #endregion
            
            public Game(UpdateCallback update,
                        InitializeCallback beforeInitialize,
                        InitializeCallback initialize,
                        InitializeCallback afterInitialize)
            {
                this.update           = update ?? throw new ArgumentNullException(nameof(update));
                this.beforeInitialize = beforeInitialize ?? throw new ArgumentNullException(nameof(beforeInitialize));
                this.initialize       = initialize ?? throw new ArgumentNullException(nameof(initialize));
                this.afterInitialize  = afterInitialize ?? throw new ArgumentNullException(nameof(afterInitialize));

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
                
                beforeInitialize();
                
                initialize();
                
                afterInitialize();
            }
        }
        #endregion
        
        #region Events
        public event EventHandler Exiting;
        #endregion
        
        #region Fields
        private readonly Game game;
        
        private readonly Kernel systems;
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
            
            systems = new Kernel(DependencyBindingOptions.Interfaces);
            game    = new Game(Update, BeforeInitialize, Initialize, AfterInitialize);
            
            game.Exiting += Game_OnExiting;
        }

        #region Event handlers
        private void Game_OnExiting(object sender, EventArgs e)
        {            
            Exiting?.Invoke(this, EventArgs.Empty);

            foreach (var system in systems.All<IGameEngineSystem>())
                system.Deinitialize();
        }
        #endregion
        
        private void Update(IGameEngineTime time)
        {
            foreach (var system in systems.All<IGameEngineSystem>())
                system.Update(time);
        }

        private void BeforeInitialize()
        {
            // Bind core systems that every game should have.
            systems.Bind(new GraphicsDeviceSystem(game.GraphicsDeviceManager));
            systems.Bind(new ContentSystem(game.Content));
            systems.Bind(new CsScriptingSystem(systems));
            systems.Bind(new InputDeviceSystem());

            // Allow game to bind game specific bindings.
            BindSystems(systems);
        }
        
        private void Initialize()
        {
            // Initialize all systems bound to the kernel.
            foreach (var system in systems.All<IGameEngineSystem>())
                system.Initialize();
        }

        private void AfterInitialize()
        {
            // Setup input devices.
            systems.First<IInputDeviceSystem>().Register(new MouseDevice(4));
            systems.First<IInputDeviceSystem>().Register(new KeyboardDevice(game.Window, 4));
            
            // Allow game to do game specific initialization.
            InitializeSystems(systems);
        }
        
        
        /// <summary>
        /// Override to bind game specific systems.
        /// </summary>
        protected virtual void BindSystems(IDependencyBinder systems)
        {
        }

        /// <summary>
        /// Override to run game specific initialization.
        /// </summary>
        protected virtual void InitializeSystems(IDependencyLocator systems)
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