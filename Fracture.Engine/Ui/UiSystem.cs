using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fracture.Engine.Core;
using Fracture.Engine.Graphics;
using Fracture.Engine.Input.Devices;
using Fracture.Engine.Ui.Controls;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Shattered.Content.Ui;

namespace Fracture.Engine.Ui
{
    /// <summary>
    /// Pipeline phase that handles rendering user interfaces.
    /// </summary>
    public sealed class UiPipelinePhase : GraphicsPipelinePhase
    {
        #region Fields
        private readonly IUiSystem uis;
        #endregion
        
        public UiPipelinePhase(IGameEngine engine, int index) 
            : base(engine, index, new GraphicsFragmentSettings(
                   SpriteSortMode.Deferred,
                   BlendState.AlphaBlend,
                   new SamplerState
                   {
                       Filter   = TextureFilter.Point,
                       AddressU = TextureAddressMode.Clamp,
                       AddressV = TextureAddressMode.Clamp,
                       AddressW = TextureAddressMode.Clamp
                   },
                   DepthStencilState.None,
                   RasterizerState.CullNone))
        {
            uis = engine.Systems.First<IUiSystem>();
        }

        public override void Execute(IGameEngineTime time)
        {
            var fragment = Pipeline.FragmentAtIndex(Index);

            foreach (var ui in uis)
            {
                ui.BeforeDraw(fragment, time);
            
                fragment.Begin(ui.View);
                
                ui.Draw(fragment, time);

                fragment.End();
   
                ui.AfterDraw(fragment, time);
            }
        }
    }
    
    public interface IUiSystem : IObjectManagementSystem, IEnumerable<Ui>
    {
        Ui Create(IStaticContainerControl root, IUiStyle style, IView view, string name);
        Ui Create(IStaticContainerControl root, IView view, string name);
        
        /// <summary>
        /// Deletes given UI from the system and disposes it.
        /// </summary>
        void Delete(Ui ui);
    }
    
    public sealed class UiSystem : ActiveGameEngineSystem, IUiSystem
    {
        #region Fields
        private readonly List<Ui> uis;

        private GraphicsDevice graphics;
        private ContentManager content;
        
        private IInputDeviceSystem devices;
        #endregion

        #region Properties
        public int Count => uis.Count;
        #endregion

        public UiSystem(int priority)
            : base(priority) => uis = new List<Ui>();

        public override void Deinitialize()
        {
            for (var i = 0; i < uis.Count; i++) uis[i].Dispose();

            uis.Clear();

            base.Deinitialize();
        }

        public Ui Create(IStaticContainerControl root, IUiStyle style, IView view, string name)
        {
            root.Style          = style ?? throw new ArgumentNullException(nameof(style));
            root.GraphicsDevice = graphics;

            var ui = new Ui(name, 
                            view, 
                            root, 
                            devices.OfType<IMouseDevice>().First(), 
                            devices.OfType<IKeyboardDevice>().First());
            
            uis.Add(ui);

            root.UpdateLayout();

            return ui;
        }

        public Ui Create(IStaticContainerControl root, IView view, string name)
            => Create(root, content.Load<UiStyle>("ui\\styles\\default"), view, name);

        public void Delete(Ui ui)
        {
            if (ui == null)
                throw new ArgumentNullException(nameof(ui));
            
            if (!uis.Remove(ui))
                throw new InvalidOperationException("could not remove ui");
            
            ui.Dispose();
        }
        
        public void Clear()
        {
            while (uis.Count != 0)
                Delete(uis[0]);
        }
        
        public override void Initialize(IGameEngine engine)
        {    
            base.Initialize(engine);

            devices = Engine.Systems.First<IInputDeviceSystem>();
            
            graphics = Engine.Services.First<GraphicsDevice>();
            content  = Engine.Services.First<ContentManager>();
        }

        public override void Update(IGameEngineTime time)
        {
            for (var i = 0; i < uis.Count; i++)
                uis[i].Update(time);
        }

        IEnumerator<Ui> IEnumerable<Ui>.GetEnumerator()
            => uis.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => uis.GetEnumerator();
    }
}
