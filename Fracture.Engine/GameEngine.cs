using System;
using System.Linq;
using Fracture.Common.Di;
using Fracture.Common.Di.Binding;
using Fracture.Engine.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fracture.Engine
{
    /// <summary>
    /// Abstract base class for implementing game engine functions
    /// and implementing games. To implement a game, inherit the engine.
    /// </summary>
    public abstract class GameEngine : Game, IGameEngine
    {
        #region Events
        public new event EventHandler Exiting;
        #endregion
        
        #region Fields
        private readonly GameEngineTime time;
        
        private readonly Kernel services;
        private readonly Kernel systems;
        #endregion
        
        #region Properties
        protected GraphicsDeviceManager GraphicsDeviceManager
        {
            get;
        }
        
        public IGameEngineTime Time
            => time;

        public IDependencyLocator Systems 
            => systems;
        
        public new IDependencyLocator Services
            => services;
        #endregion

        protected GameEngine()
        {
            time = new GameEngineTime();
            
            services = new Kernel(DependencyBindingOptions.Class | 
                                  DependencyBindingOptions.Interfaces | 
                                  DependencyBindingOptions.Strict);
            
            systems = new Kernel(DependencyBindingOptions.Interfaces | DependencyBindingOptions.Strict);
            
            systems.Bind(this);
            
            // Mono game specific initialization.
            services.Bind(GraphicsDeviceManager = new GraphicsDeviceManager(this)
            {
                GraphicsProfile = GraphicsProfile.HiDef
            });
            
            Content.RootDirectory = "Content\\bin";
        }
        
        protected virtual void BindSystems(IDependencyBinder binder)
        {
        }
        
        protected virtual void BindServices(IDependencyBinder binder)
        {
        }

        protected override void Initialize()
        {
            base.Initialize();
         
            services.Bind(GraphicsDevice);
            services.Bind(Content);
            
            BindServices(services);
            BindSystems(systems);
            
            foreach (var system in systems.All<IGameEngineSystem>())
                system.Initialize();
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            Exiting?.Invoke(this, EventArgs.Empty);
            
            base.OnExiting(sender, args);
            
            foreach (var system in systems.All<IGameEngineSystem>())
                system.Deinitialize();
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            
            time.Elapsed = gameTime.ElapsedGameTime;
            time.Total   = gameTime.TotalGameTime;
            
            foreach (var system in systems.All<IActiveGameEngineSystem>().OrderBy(s => s.Priority))
                system.Update();
            
            time.Frame++;
        }

        #region Sealed members
        protected sealed override void Draw(GameTime gameTime)
        {
            // NOP.
            base.Draw(gameTime);
        }

        protected sealed override bool BeginDraw()
        {
            // NOP.
            return base.BeginDraw();
        }

        protected sealed override void BeginRun()
        {
            // NOP.
            base.BeginRun();
        }

        protected sealed override void EndDraw()
        {
            // NOP.
            base.EndDraw();
        }

        protected sealed override void EndRun()
        {
            // NOP.
            base.EndRun();
        }

        protected sealed override void LoadContent()
        {
            // NOP.
            base.LoadContent();
        }

        protected sealed override void UnloadContent()
        {
            // NOP.
            base.UnloadContent();
        }
        #endregion
    }
}