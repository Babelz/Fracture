using Fracture.Common.Di;
using Fracture.Engine;
using Fracture.Engine.Core;
using Fracture.Engine.Core.Primitives;
using Fracture.Engine.Ecs;
using Fracture.Engine.Events;
using Fracture.Engine.Graphics;
using Fracture.Engine.Input;
using Fracture.Engine.Input.Devices;
using Fracture.Engine.Physics;
using Fracture.Engine.Scripting;
using Fracture.Engine.Ui;

namespace Fracture.Client
{
    public sealed class ShatteredWorldGame : GameEngine 
    {
        public ShatteredWorldGame()
        {
            IsMouseVisible = true;
            
            GraphicsDeviceManager.PreferredBackBufferWidth  = 1024;
            GraphicsDeviceManager.PreferredBackBufferHeight = 768;
            
            GraphicsDeviceManager.ApplyChanges();
        }
        
        protected override void BindSystems(IDependencyBinder binder)
        {
            // Input systems initialization.
            binder.Bind(new InputDeviceSystem(SystemPriorityHint.Beginning + 1, 
                                              new MouseDevice(4), 
                                              new KeyboardDevice(Window, 4)));
         
            binder.Bind(new MouseInputSystem(SystemPriorityHint.Beginning + 2));
            binder.Bind(new KeyboardInputSystem(SystemPriorityHint.Beginning + 2));
            
            // ECS non-prioritized system initialization.
            binder.Bind<EntitySystem>();
            binder.Bind<TransformComponentComponentSystem>();
            binder.Bind<GraphicsLayerSystem>();
            binder.Bind<ViewSystem>();
            
            // Physics system initialization.
            binder.Bind(new PhysicsWorldSystem(new FixedPhysicsSimulationTime(PhysicsWorldSystem.DefaultDelta), 
                16, 
                8, 
                SystemPriorityHint.Middle + 1)
            {
                Gravity = 9.81f 
            });
            
            binder.Bind(new PhysicsBodyComponentSystem(SystemPriorityHint.Middle + 2));
            
            // Event system initialization. Make sure game logic runs before updating
            // events or we are in frame desync.
            binder.Bind(new EventQueueSystem(SystemPriorityHint.Last + 2));
            binder.Bind(new EventSchedulerSystem(SystemPriorityHint.Last + 2));

            // Graphics system initialization. Make sure graphical systems are last 
            // or we are in frame desync.
            binder.Bind(new SpriteComponentSystem(SystemPriorityHint.Last + 3));
            binder.Bind(new SpriteAnimationComponentSystem(SystemPriorityHint.Last + 3));
            binder.Bind(new QuadComponentSystem(SystemPriorityHint.Last + 3));

            binder.Bind(new GraphicsPipelineSystem(SystemPriorityHint.Last + 4));
        
            // UI system initialization. UIs are always drawn over the world.
            binder.Bind(new UiSystem(SystemPriorityHint.Last + 1));
        }

        protected override void BindServices(IDependencyBinder binder)
        {
            binder.Bind(new CsScriptRepositoryFactory());
        }

        protected override void Initialize()
        {
            base.Initialize();
            
            // Set presentation parameters.
            Transform.SetScreenUnitToWorldUnitRatio(32.0f);
            
            UiCanvas.SetSize(GraphicsDevice.PresentationParameters.BackBufferWidth, 
                             GraphicsDevice.PresentationParameters.BackBufferHeight);
            
            // Setup phases.
            var pipeline = Systems.First<IGraphicsPipelineSystem>();
            
            pipeline.AddPhase(new GraphicsLayerPipelinePhase(this, 0));
            pipeline.AddPhase(new UiPipelinePhase(this, 1));

            // Create layers.   
            var layers = Systems.First<IGraphicsLayerSystem>();
            
            layers.Create(Environment.GraphicsLayers.Back, 0);
            layers.Create(Environment.GraphicsLayers.Middle, 1);
            layers.Create(Environment.GraphicsLayers.Front, 2);
        }
    }
}