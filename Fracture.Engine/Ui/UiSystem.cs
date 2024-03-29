﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fracture.Common.Di.Attributes;
using Fracture.Engine.Core;
using Fracture.Engine.Core.Systems;
using Fracture.Engine.Graphics;
using Fracture.Engine.Input.Devices;
using Fracture.Engine.Ui.Controls;
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
        private readonly IUiSystem   uis;
        private readonly IViewSystem views;
        #endregion

        public UiPipelinePhase(IGameHost host, IGraphicsPipelineSystem pipelines, IUiSystem uis, IViewSystem views, int index)
            : base(host,
                   pipelines,
                   index,
                   new GraphicsFragmentSettings(
                       SpriteSortMode.Deferred,
                       BlendState.NonPremultiplied,
                       new SamplerState
                       {
                           Filter   = TextureFilter.Point,
                           AddressU = TextureAddressMode.Clamp,
                           AddressV = TextureAddressMode.Clamp,
                           AddressW = TextureAddressMode.Clamp,
                       },
                       DepthStencilState.None,
                       RasterizerState.CullNone))
        {
            this.uis   = uis ?? throw new ArgumentNullException(nameof(uis));
            this.views = views ?? throw new ArgumentNullException(nameof(views));
        }

        public override void Execute(IGameEngineTime time)
        {
            var fragment = Pipeline.FragmentAtIndex(Index);

            foreach (var ui in uis)
            {
                ui.BeforeDraw(fragment, time);

                foreach (var view in views.Where(v => v.Space == ViewSpace.Screen))
                {
                    fragment.Begin(view);

                    ui.Draw(fragment, time);

                    fragment.End();
                }
                
                ui.AfterDraw(fragment, time);
            }
        }
    }

    public interface IUiSystem : IObjectManagementSystem, IEnumerable<UiContainer>
    {
        UiContainer Create(IStaticContainerControl root, IUiStyle style, IView view, string name);
        UiContainer Create(IStaticContainerControl root, IView view, string name);

        /// <summary>
        /// Deletes given UI from the system and disposes it.
        /// </summary>
        void Delete(UiContainer container);
    }

    public sealed class UiSystem : GameEngineSystem, IUiSystem
    {
        #region Fields
        private readonly List<UiContainer> uis;

        private readonly IGraphicsDeviceSystem graphics;

        private readonly IInputDeviceSystem devices;

        private readonly IContentSystem content;
        #endregion

        #region Properties
        public int Count => uis.Count;
        #endregion

        [BindingConstructor]
        public UiSystem(IInputDeviceSystem devices, IGraphicsDeviceSystem graphics, IContentSystem content)
        {
            this.graphics = graphics;
            this.devices  = devices;
            this.content  = content;

            uis = new List<UiContainer>();
        }

        public override void Deinitialize()
        {
            for (var i = 0; i < uis.Count; i++)
                uis[i].Dispose();

            uis.Clear();

            base.Deinitialize();
        }

        public UiContainer Create(IStaticContainerControl root, IUiStyle style, IView view, string name)
        {
            root.Style    = style ?? throw new ArgumentNullException(nameof(style));
            root.Graphics = graphics;

            var ui = new UiContainer(name,
                                     view,
                                     root,
                                     devices.OfType<IMouseDevice>().First(),
                                     devices.OfType<IKeyboardDevice>().First());

            uis.Add(ui);

            root.UpdateLayout();

            return ui;
        }

        public UiContainer Create(IStaticContainerControl root, IView view, string name)
            => Create(root, content.Load<UiStyle>("ui\\styles\\default"), view, name);

        public void Delete(UiContainer container)
        {
            if (container == null)
                throw new ArgumentNullException(nameof(container));

            if (!uis.Remove(container))
                throw new InvalidOperationException("could not remove ui");

            container.Dispose();
        }

        public void Clear()
        {
            while (uis.Count != 0)
                Delete(uis[0]);
        }

        public override void Update(IGameEngineTime time)
        {
            for (var i = 0; i < uis.Count; i++)
                uis[i].Update(time);
        }

        IEnumerator<UiContainer> IEnumerable<UiContainer>.GetEnumerator()
            => uis.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => uis.GetEnumerator();
    }
}