using System;
using System.Collections.Generic;
using System.Linq;
using Fracture.Engine.Core;
using Fracture.Engine.Graphics;
using Fracture.Engine.Input.Devices;
using Fracture.Engine.Ui.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shattered.Content.Ui;

namespace Fracture.Engine.Ui.Controls
{
    /// <summary>
    /// Interface for implementing container controls that can contain
    /// children components and are static in nature meaning new controls
    /// can't be explicitly be added to them.
    /// </summary>
    public interface IStaticContainerControl : IControl, IControlEnumerator, IDisposable
    {
        #region Properties
        /// <summary>
        /// Sets or gets the render target property of the
        /// control. This controls if the control should render
        /// it self and its children to a render target.
        /// </summary>
        bool UseRenderTarget
        {
            get;
            set;
        }

        /// <summary>
        /// Returns the render target of this control
        /// or null if control is not using render targets.
        /// </summary>
        RenderTarget2D RenderTarget
        {
            get;
        }

        /// <summary>
        /// When using render targets, use this property to navigate inside the
        /// render target.
        /// </summary>
        Vector2 ViewOffset
        {
            get;
            set;
        }

        /// <summary>
        /// When using render targets, use this property to
        /// determine the max view area inside the render target. 
        /// </summary>
        Vector2 ViewSafePosition
        {
            get;
        }
        
        /// <summary>
        /// Gets or sets the graphics device associated
        /// with this control.
        /// </summary>
        GraphicsDevice GraphicsDevice
        {
            get;
            set;
        }
        #endregion

        /// <summary>
        /// Forces layout update for all children contained
        /// in the container.
        /// </summary>
        void UpdateChildrenLayout();

        void Clear();
    }
    
    /// <summary>
    /// Base class for implementing container controls.
    /// </summary>
    public abstract class StaticContainerControl : Control, IStaticContainerControl
    {
        #region Fields
        private bool suppressRenderTargetRender;
        
        private bool useRenderTarget;

        private bool disposed;
        
        private GraphicsDevice graphicsDevice;
        #endregion

        #region Events
        public event EventHandler<ControlEventArgs> ControlAdded;
        public event EventHandler<ControlEventArgs> ControlRemoved;
        #endregion

        #region Properties
        protected IControlManager Children
        {
            get;
        }

        public int ControlsCount => Children.ControlsCount;

        public bool UseRenderTarget
        {
            get => useRenderTarget;
            set
            {
                var old = useRenderTarget;

                useRenderTarget = value;

                if (useRenderTarget && !old) UpdateRenderTarget();
                else                         DeinitializeRenderTarget();    
            }
        }

        public RenderTarget2D RenderTarget
        {
            get;
            private set;
        }

        public Vector2 ViewOffset
        {
            get;
            set;
        }

        public Vector2 ViewSafePosition
        {
            get
            {
                var x = Controls.Min(c => c.ActualBoundingBox.X);
                var y = Controls.Min(c => c.ActualBoundingBox.Y);
                var r = Controls.Max(c => c.ActualBoundingBox.Right);
                var b = Controls.Max(c => c.ActualBoundingBox.Bottom);
                
                return new Vector2(r - x - ActualBoundingBox.Width, b - y - ActualBoundingBox.Height);
            }
        }

        public GraphicsDevice GraphicsDevice
        {
            get => graphicsDevice;
            set
            {
                graphicsDevice = value;

                if (useRenderTarget)
                    UpdateRenderTarget();

                foreach (var container in Children.Controls.Where(c => c is IStaticContainerControl)
                                                           .Cast<IStaticContainerControl>())
                {
                    container.GraphicsDevice = value;
                }
            }
        }

        public override IUiStyle Style
        {
            get => base.Style;
            set
            {
                base.Style = value;

                for (var i = 0; i < ControlsCount; i++) this[i].Style = value;
            }
        }

        public IEnumerable<IControl> Controls => Children.Controls;
        #endregion

        #region Indexers
        public IControl this[int index] => Children[index];
        #endregion

        protected StaticContainerControl(IControlManager children)
        {
            Children = children;

            Children.ControlAdded   += Controls_ControlAdded;
            Children.ControlRemoved += Controls_ControlRemoved;

            LayoutChanged += ContainerControl_LayoutChanged;
        }

        protected StaticContainerControl()
            : this(new ControlManager())
        {
        }

        #region Event handlers
        private void ContainerControl_LayoutChanged(object sender, EventArgs e)
            => UpdateChildrenLayout();

        private void Controls_ControlRemoved(object sender, ControlEventArgs e)
        {
            ControlRemoved?.Invoke(this, e);

            var control = e.Control;

            control.ParentChanged -= Control_ParentChanged;
            control.LayoutChanged -= Control_LayoutChanged;

            control.Parent = null;
            control.Style  = null;

            if (control is IStaticContainerControl container) container.GraphicsDevice = null;

            if (useRenderTarget) UpdateRenderTarget();

            UpdateChildrenLayout();
        }

        private void Controls_ControlAdded(object sender, ControlEventArgs e)
        {
            ControlAdded?.Invoke(this, e);

            var control = e.Control;

            control.ParentChanged += Control_ParentChanged;
            control.LayoutChanged += Control_LayoutChanged;

            control.Parent = this;
            control.Style  = Style;

            if (control is IStaticContainerControl container) container.GraphicsDevice = graphicsDevice;
            
            UpdateChildrenLayout();
        }

        private void Control_LayoutChanged(object sender, EventArgs e)
        {
            if (useRenderTarget) UpdateRenderTarget();
        }
        
        private void Control_ParentChanged(object sender, ControlParentEventArgs e)
        {
            var control = (IControl)sender;

            if (!ReferenceEquals(e.NextParent, this))
                Children.Remove(control);
        }
        #endregion

        private void DeinitializeRenderTarget()
        {
            RenderTarget.Dispose();

            RenderTarget = null;
        }

        private void UpdateRenderTarget()
        {
            if (RenderTarget != null)
            {
                RenderTarget.Dispose();

                RenderTarget = null;
            }

            if (graphicsDevice == null)
                return;

            var bounds = UiCanvas.ToScreenUnits(ActualBoundingBox.Bounds);

            RenderTarget = new RenderTarget2D(GraphicsDevice,
                                              (int)Math.Floor(bounds.X),
                                              (int)Math.Floor(bounds.Y),
                                              false,
                                              SurfaceFormat.Color,
                                              DepthFormat.None,
                                              GraphicsDevice.PresentationParameters.MultiSampleCount,
                                              RenderTargetUsage.DiscardContents);
        }
        
        /// <summary>
        /// Method called when control is being disposed and managed
        /// resources should be disposed. Remember to call the 
        /// base method in inheriting classes to avoid resource leaks.
        /// </summary>
        protected virtual void DisposeManaged()
        {
            if (RenderTarget == null) return;
            
            RenderTarget.Dispose();

            RenderTarget = null;
        }

        /// <summary>
        /// Method called when control is being disposed and unmanaged 
        /// resources should be disposed. Remember to call the 
        /// base method in inheriting classes to avoid resource leaks.
        /// </summary>
        protected virtual void DisposeUnmanaged()
        {
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                throw new InvalidOperationException($"control {(string.IsNullOrEmpty(Id) ? GetType().Name : $"of type {GetType().Name} with id {Id}")} already disposed");

            if (disposing)
                DisposeManaged();

            DisposeUnmanaged();

            disposed = true;
        }

        protected override void InternalUpdate(IGameEngineTime time)
        {
            for (var i = 0; i < ControlsCount; i++) this[i].Update(time);
        }
        
        /// <summary>
        /// Calls before render for all child controls.
        /// </summary>
        protected override void InternalBeforeDraw(IGraphicsFragment fragment, IGameEngineTime time)
        {
            for (var i = 0; i < ControlsCount; i++) this[i].BeforeDraw(fragment, time);

            if (!UseRenderTarget) return;
            
            fragment.Begin(RenderTarget, 
                           new Viewport(0, 0, RenderTarget.Width, RenderTarget.Height),
                           View.CreateViewMatrix(
                               UiCanvas.ToScreenUnits(ActualBoundingBox.Position) + 
                               new Vector2(RenderTarget.Width * 0.5f, RenderTarget.Height * 0.5f) + 
                               UiCanvas.ToScreenUnits(ViewOffset),
                               new Vector2(RenderTarget.Width, RenderTarget.Height), 
                               0.0f, 
                               1.0f));
                
            suppressRenderTargetRender = true;

            InternalDraw(fragment, time);

            suppressRenderTargetRender = false;
                
            fragment.End();
        }
        
        /// <summary>
        /// Calls render for all child controls.
        /// </summary>
        protected override void InternalDraw(IGraphicsFragment pipeline, IGameEngineTime time)
        {
            if (useRenderTarget && !suppressRenderTargetRender)
            {
                pipeline.DrawSprite(UiCanvas.ToScreenUnits(ActualBoundingBox.Position),
                                    Vector2.One,
                                    0.0f,
                                    Vector2.Zero,
                                    new Vector2(RenderTarget.Width, RenderTarget.Height),
                                    RenderTarget,
                                    Color.White);
                                    
            }
            else
            {
                for (var i = 0; i < ControlsCount; i++) this[i].Draw(pipeline, time);
            }
        }

        /// <summary>
        /// Calls after render for all child controls.
        /// </summary>
        protected override void InternalAfterDraw(IGraphicsFragment fragment, IGameEngineTime time)
        {
            for (var i = 0; i < ControlsCount; i++) this[i].AfterDraw(fragment, time);
        }

        protected override void InternalReceiveKeyboardInput(IGameEngineTime time, IKeyboardDevice keyboard)
        {   
            for (var i = 0; i < ControlsCount; i++) this[i].ReceiveKeyboardInput(time, keyboard);
        }

        protected override void InternalReceiveMouseInput(IGameEngineTime time, IMouseDevice mouse)
        {
            for (var i = 0; i < ControlsCount; i++) this[i].ReceiveMouseInput(time, mouse);
        }

        protected override void InternalReceiveTextInput(IGameEngineTime time, string text)
        {
            for (var i = 0; i < ControlsCount; i++) this[i].ReceiveTextInput(time, text);
        }
       
        public override void UpdateLayout()
        {
            base.UpdateLayout();

            UpdateChildrenLayout();
        }

        public override void Enable()
        {
            base.Enable();

            if (!Enabled) return;
            
            for (var i = 0; i < Children.ControlsCount; i++) Children[i].Enable();
        }
        public override void Disable()
        {
            base.Disable();

            if (Enabled) return;
            
            for (var i = 0; i < Children.ControlsCount; i++) Children[i].Disable();
        }

        public override void Show()
        {
            base.Show();

            if (!Visible) return;
            
            for (var i = 0; i < Children.ControlsCount; i++) Children[i].Show();
        }
        public override void Hide()
        {
            base.Hide();

            if (Visible) return;
            
            for (var i = 0; i < Children.ControlsCount; i++) Children[i].Hide();
        }
        
        public virtual void UpdateChildrenLayout()
        {
            for (var i = 0; i < Children.ControlsCount; i++) Children[i].UpdateLayout();
        }

        public virtual void Clear() => Children.Clear();

        public void Dispose()
        {
            GC.SuppressFinalize(this);

            Dispose(true);        
        }
    }
}
