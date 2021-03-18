using System;
using Fracture.Common;
using Fracture.Engine.Core;
using Fracture.Engine.Core.Primitives;
using Fracture.Engine.Graphics;
using Fracture.Engine.Input.Devices;
using Fracture.Engine.Ui.Components;
using Microsoft.Xna.Framework;

namespace Fracture.Engine.Ui.Controls
{
    public sealed class ControlEventArgs : EventArgs
    {
        #region Properties
        public IControl Control
        {
            get;
        }
        #endregion

        public ControlEventArgs(IControl control)
            => Control = control;
    }

    public sealed class ControlParentEventArgs : EventArgs
    {
        #region Properties
        public IControl LastParent
        {
            get;
        }
        public IControl NextParent
        {
            get;
        }
        #endregion

        public ControlParentEventArgs(IControl lastParent, IControl nextParent)
        {
            LastParent = lastParent;
            NextParent = nextParent;
        }
    }

    public sealed class ControlMouseInputEventArgs : EventArgs
    {
        #region Properties
        public ControlMouseInputManager MouseInputManager
        {
            get;
        }
        #endregion

        public ControlMouseInputEventArgs(ControlMouseInputManager mouseInputManager)
            => MouseInputManager = mouseInputManager;
    }

    public sealed class ControlKeyboardInputEventArgs : EventArgs
    {
        #region Properties
        public ControlKeyboardInputManager KeyboardInputManager
        {
            get;
        } 
        #endregion

        public ControlKeyboardInputEventArgs(ControlKeyboardInputManager keyboardInputManager)
            => KeyboardInputManager = keyboardInputManager;
    }

    public sealed class ControlTextInputEventArgs : EventArgs
    {
        #region Properties
        public string Input
        {
            get;
        } 
        #endregion

        public ControlTextInputEventArgs(string input)
            => Input = input;
    }
    
    /// <summary>
    /// Interface creating controls. Controls define
    /// their transform, parent-child relation and events they use.
    /// </summary>
    public interface IControl
    {
        #region Events
        event EventHandler<ControlParentEventArgs> ParentChanged;

        event EventHandler VisibilityChanged;
        event EventHandler EnabledChanged;

        event EventHandler BoundingBoxChanged;
        event EventHandler PositionChanged;
        event EventHandler SizeChanged;

        event EventHandler ActualBoundingBoxChanged;
        event EventHandler ActualPositionChanged;
        event EventHandler ActualSizeChanged;

        event EventHandler PaddingChanged;
        event EventHandler MarginChanged;

        event EventHandler LayoutChanged;
        
        event EventHandler FocusChanged;

        event EventHandler<ControlKeyboardInputEventArgs> KeyboardInputReceived;
        event EventHandler<ControlMouseInputEventArgs> MouseInputReceived;
        event EventHandler<ControlTextInputEventArgs> TextInputReceived;

        event EventHandler KeyboardInputEnabledChanged;
        event EventHandler MouseInputEnabledChanged;
        event EventHandler TextInputEnabledChanged;

        event EventHandler StyleChanged;

        event EventHandler<ControlMouseInputEventArgs> MouseEnter;
        event EventHandler<ControlMouseInputEventArgs> MouseHover;
        event EventHandler<ControlMouseInputEventArgs> MouseLeave;

        event EventHandler<ControlMouseInputEventArgs> Drag;
        #endregion

        #region Properties
        /// <summary>
        /// Id of the control. 
        /// </summary>
        string Id
        {
            get;
            set;
        }

        /// <summary>
        /// Custom user data of this control.
        /// </summary>
        object UserData
        {
            get;
            set;
        }

        /// <summary>
        /// Position of the control. Relative to the parent.
        /// </summary>
        Vector2 Position
        {
            get;
            set;
        }
        /// <summary>
        /// Actual position of the control. Actual position on the screen,
        /// not relative to the parent.
        /// </summary>
        Vector2 ActualPosition
        {
            get;
            set;
        }
        
        /// <summary>
        /// Size of the control. Relative to the parent.
        /// </summary>
        Vector2 Size
        {
            get;
            set;
        }
        /// <summary>
        /// Actual size of the control. Actual size on the screen,
        /// not relative to the parent.
        /// </summary>
        Vector2 ActualSize
        {
            get;
            set;
        }

        /// <summary>
        /// Bounding box of the control. Relative to the parent.
        /// </summary>
        Rectf BoundingBox
        {
            get;
        }
        /// <summary>
        /// Actual bounding box of the control. Actual bounding box on
        /// the screen, not relative to the parent.
        /// </summary>
        Rectf ActualBoundingBox
        {
            get;
        }

        /// <summary>
        /// Returns the collision bounding box of this control. If the control
        /// is inside render target, transform will be applied.
        /// </summary>
        Rectf CollisionBoundingBox
        {
            get;
        }

        /// <summary>
        /// Margin offset of the control.
        /// </summary>
        UiOffset Margin
        {
            get;
            set;
        }

        /// <summary>
        /// Padding offset of the control.
        /// </summary>
        UiOffset Padding
        {
            get;
            set;
        }

        /// <summary>
        /// Anchor of this control.
        /// </summary>
        Anchor Anchor
        {
            get;
            set;
        }
        /// <summary>
        /// Positioning mode of this control.
        /// </summary>
        Positioning Positioning
        {
            get;
            set;
        }

        /// <summary>
        /// Style of the control.
        /// </summary>
        IUiStyle Style
        {
            get;
            set;
        }
        /// <summary>
        /// Prent control of this control.
        /// </summary>
        IControl Parent
        {
            get;
            set;
        }

        /// <summary>
        /// Does the control have focus.
        /// </summary>
        bool HasFocus
        {
            get;
        }

        /// <summary>
        /// Is this control visible in the view
        /// of the parent. If the parent uses render target this value 
        /// returns true if the control is in the visible area of the target. If no 
        /// render target is in use, returns false.
        /// </summary>
        bool VisibleFromParent
        {
            get;
        }

        /// <summary>
        /// Is the control visible.
        /// </summary>
        bool Visible
        {
            get;
        }
        /// <summary>
        /// Is the control enabled.
        /// </summary>
        bool Enabled
        {
            get;
        }

        /// <summary>
        /// Does the control accept keyboard input.
        /// </summary>
        bool AcceptsKeyboardInput
        {
            get;
        }

        /// <summary>
        /// Does the control accept mouse input.
        /// </summary>
        bool AcceptsMouseInput
        {
            get;
        }

        bool AcceptsTextInput
        {
            get;
        }
        
        bool Draggable
        {
            get;
        }

        /// <summary>
        /// Tab index of the control.
        /// </summary>
        int TabIndex
        {
            get;
            set;
        }
        #endregion

        /// <summary>
        /// Forces the control to update it's layout.
        /// </summary>
        void UpdateLayout();

        void ResumeKeyboardInputUpdate();
        void ResumeMouseInputUpdate();
        void ResumeTextInputUpdate();

        void SuspendKeyboardInputUpdate();
        void SuspendMouseInputUpdate();
        void SuspendTextInputUpdate();

        ///// <summary>
        ///// Disable focusing for the control. 
        ///// </summary>
        //void IgnoreFocus();

        ///// <summary>
        ///// Enables focusing for the control.
        ///// </summary>
        //void AcceptFocus();

        /// <summary>
        /// Enables control be dragged. Does not work with <see cref="Engine.Ui.Anchor"/>.
        /// </summary>
        void EnableDrag();

        /// <summary>
        /// Disables control drag.
        /// </summary>
        void DisableDrag();

        /// <summary>
        /// Focuses this control.
        /// </summary>
        void Focus();
        /// <summary>
        /// Removes focus from this control.
        /// </summary>
        void Defocus();

        /// <summary>
        /// Enables this control allowing it to perform updates.
        /// </summary>
        void Enable();
        /// <summary>
        /// Disables this control disallowing it to perform any updates.
        /// </summary>
        void Disable();

        /// <summary>
        /// Makes this control visible.
        /// </summary>
        void Show();
        /// <summary>
        /// Makes this control hidden.
        /// </summary>
        void Hide();

        /// <summary>
        /// Apply mouse input to this control.
        /// </summary>
        void ReceiveMouseInput(IGameEngineTime time, IMouseDevice mouse);
        /// <summary>
        /// Apply keyboard input for this control.
        /// </summary>
        void ReceiveKeyboardInput(IGameEngineTime time, IKeyboardDevice keyboard);
        /// <summary>
        /// Apply text input for this control.
        /// </summary>
        void ReceiveTextInput(IGameEngineTime time, string text);

        /// <summary>
        /// Allows the control to do general updates
        /// not related to rendering or input handling directly.
        /// </summary>
        void Update(IGameEngineTime time);

        /// <summary>
        /// Called before the control gets to perform rendering.
        /// </summary>
        void BeforeDraw(IGraphicsFragment fragment, IGameEngineTime time);

        /// <summary>
        /// Called when the control is allowed to render.
        /// </summary>
        void Draw(IGraphicsFragment fragment, IGameEngineTime time);
    
        /// <summary>
        /// Called when the control is done rendering.
        /// </summary>
        void AfterDraw(IGraphicsFragment fragment, IGameEngineTime time);
    }
        
    public abstract class Control : IControl
    {
        #region Fields
        private IUiStyle style;

        private Vector2 position;
        private Vector2 actualPosition;
        
        private Rectf actualBoundingBox;
        private Rectf boundingBox;

        private Vector2 size;
        private Vector2 actualSize;
        
        private Positioning positioning;
        private Anchor anchor;

        private UiOffset margin;
        private UiOffset padding;

        private bool hover;

        private IControl parent;
        #endregion

        #region Events
        public event EventHandler<ControlParentEventArgs> ParentChanged;

        public event EventHandler VisibilityChanged;
        public event EventHandler EnabledChanged;

        public event EventHandler BoundingBoxChanged;
        public event EventHandler PositionChanged;
        public event EventHandler SizeChanged;

        public event EventHandler ActualBoundingBoxChanged;
        public event EventHandler ActualPositionChanged;
        public event EventHandler ActualSizeChanged;

        public event EventHandler PaddingChanged;
        public event EventHandler MarginChanged;

        public event EventHandler LayoutChanged;

        public event EventHandler FocusChanged;

        public event EventHandler<ControlKeyboardInputEventArgs> KeyboardInputReceived;
        public event EventHandler<ControlMouseInputEventArgs> MouseInputReceived;
        public event EventHandler<ControlTextInputEventArgs> TextInputReceived;

        public event EventHandler KeyboardInputEnabledChanged;
        public event EventHandler MouseInputEnabledChanged;
        public event EventHandler TextInputEnabledChanged;

        public event EventHandler StyleChanged;

        public event EventHandler<ControlMouseInputEventArgs> MouseEnter;
        public event EventHandler<ControlMouseInputEventArgs> MouseHover;
        public event EventHandler<ControlMouseInputEventArgs> MouseLeave;

        public event EventHandler<ControlMouseInputEventArgs> Drag;
        #endregion

        #region Properties
        protected virtual string Input
        {
            get;
            private set;
        }

        protected ControlKeyboardInputManager Keyboard
        {
            get;
        }

        protected ControlMouseInputManager Mouse
        {
            get;
        }

        public string Id
        {
            get;
            set;
        }

        public object UserData
        {
            get;
            set;
        }

        public virtual Vector2 Position
        {
            get => position;
            set
            {
                var old = position;

                position = value;

                if (position == old) return;
                
                UpdatePosition();

                UpdateBoundingBox();

                PositionChanged?.Invoke(this, EventArgs.Empty);

                LayoutChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public virtual Vector2 ActualPosition
        {
            get => actualPosition;
            set
            {
                var old = actualPosition;

                actualPosition = value;

                if (actualPosition == old) return;
                
                UpdatePosition(true);

                UpdateBoundingBox();

                ActualPositionChanged?.Invoke(this, EventArgs.Empty);

                LayoutChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public virtual Vector2 Size
        {
            get => size;
            set
            {
                var old = size;

                size = value;

                if (size == old) return;
                
                UpdateSize();

                UpdatePosition();

                UpdateBoundingBox();

                SizeChanged?.Invoke(this, EventArgs.Empty);

                LayoutChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public virtual Vector2 ActualSize
        {
            get => actualSize;
            set
            {
                var old = actualSize;

                actualSize = value;

                if (actualSize == old) return;
                
                UpdateSize(true);

                UpdatePosition();

                UpdateBoundingBox();

                ActualSizeChanged?.Invoke(this, EventArgs.Empty);

                LayoutChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public virtual Rectf BoundingBox       => boundingBox;
        public virtual Rectf ActualBoundingBox => actualBoundingBox;

        public Rectf CollisionBoundingBox
        {
            get
            {
                if (!(parent is IStaticContainerControl container) || !container.UseRenderTarget)
                    return actualBoundingBox;
                
                var offset       = container.ViewOffset;
                var myBounds     = ActualBoundingBox;
                var parentBounds = parent.ActualBoundingBox;

                myBounds = new Rectf(myBounds.Position - offset, myBounds.Bounds);

                var x = MathHelper.Clamp(myBounds.X, parentBounds.Left, parentBounds.Right);
                var y = MathHelper.Clamp(myBounds.Y, parentBounds.Top, parentBounds.Bottom);

                var w = 0.0f;
                var h = 0.0f;

                // Clamp vertical.
                if (myBounds.Top < parentBounds.Top)
                {
                    h = myBounds.Bottom - parentBounds.Top;
                    y = parentBounds.Top;
                }
                else if (myBounds.Bottom > parentBounds.Bottom)
                {
                    h = parentBounds.Bottom - myBounds.Top;
                }
                else
                {
                    h = myBounds.Height;
                }

                // Clamp horizontal.
                if (myBounds.Left < parentBounds.Left)
                {
                    w = myBounds.Right - parentBounds.Left;
                    x = parentBounds.Left;
                }
                else if (myBounds.Right > parentBounds.Right)
                {
                    w = parentBounds.Right - myBounds.Left;
                }
                else
                {
                    w = myBounds.Width;
                }
                    
                return new Rectf(new Vector2(x, y), new Vector2(w, h));
            }
        }

        public virtual UiOffset Margin
        {
            get => margin;
            set
            {
                var old = margin;

                margin = value;

                if (margin != old)
                {
                    UpdateSize();

                    UpdatePosition();

                    UpdateBoundingBox();

                    MarginChanged?.Invoke(this, EventArgs.Empty);

                    LayoutChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        public virtual UiOffset Padding
        {
            get => padding;
            set
            {
                var old = padding;

                padding = value;

                if (padding == old) return;
                
                UpdateSize();

                UpdatePosition();

                UpdateBoundingBox();

                PaddingChanged?.Invoke(this, EventArgs.Empty);

                LayoutChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public virtual Anchor Anchor
        {
            get => anchor;
            set
            {
                var old = anchor;

                anchor = value;

                if (anchor == old || Positioning != Positioning.Anchor) return;
                
                UpdateAnchor();

                UpdateBoundingBox();

                LayoutChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public virtual Positioning Positioning
        {
            get => positioning;
            set
            {
                var old = positioning;

                positioning = value;

                if (positioning == old) return;
                
                UpdatePosition();

                UpdateBoundingBox();

                LayoutChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        
        public virtual IUiStyle Style
        {
            get => style;
            set
            {
                var old = style;

                style = value;

                if (style != old)
                    StyleChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public IControl Parent
        {
            get => parent;
            set
            {
                var old = parent;

                parent = value;

                if (parent == old) return;
                
                UpdateSize();

                UpdatePosition();

                UpdateBoundingBox();

                ParentChanged?.Invoke(this, new ControlParentEventArgs(old, parent));
            }
        }

        public bool HasFocus
        {
            get;
            private set;
        }

        public bool VisibleFromParent
        {
            get
            {
                if (!(parent is IStaticContainerControl container) || !container.UseRenderTarget) 
                    return true;
                
                var offset       = container.ViewOffset;
                var myBounds     = ActualBoundingBox;
                var parentBounds = parent.ActualBoundingBox;

                myBounds = new Rectf(myBounds.Position - offset, myBounds.Bounds);

                return Rectf.Intersects(parentBounds, myBounds);
            }
        }

        public bool Visible
        {
            get;
            private set;
        }

        public bool Enabled
        {
            get;
            private set;
        }

        public bool AcceptsKeyboardInput
        {
            get;
            private set;
        }

        public bool AcceptsMouseInput
        {
            get;
            private set;
        }

        public bool AcceptsTextInput
        {
            get;
            private set;
        }

        public bool Draggable
        {
            get;
            private set;
        }

        public int TabIndex
        {
            get;
            set;
        }
        #endregion

        protected Control()
        {
            Mouse    = new ControlMouseInputManager();
            Keyboard = new ControlKeyboardInputManager();
            Size     = Vector2.One;

            Id = string.Empty;

            Visible = true;
            Enabled = true;

            AcceptsKeyboardInput = true;
            AcceptsMouseInput    = true;
            AcceptsTextInput     = true;

            Draggable = false;

            // -1 index indicates that tab indices are not used for focus management.
            TabIndex = -1;
        }

        protected void UpdateMouseState()
        {
            if (!hover && Mouse.IsHovering(this))
            {
                MouseEnter?.Invoke(this, new ControlMouseInputEventArgs(Mouse));

                hover = true;
            }
            else if (hover)
            {
                if (Mouse.IsHovering(this))
                {
                    MouseHover?.Invoke(this, new ControlMouseInputEventArgs(Mouse));
                }
                else
                {
                    MouseLeave?.Invoke(this, new ControlMouseInputEventArgs(Mouse));

                    hover = false;
                }
            }
        }

        private void UpdateMouseDrag()
        {
            if (!HasFocus)               return;
            if (!Mouse.IsHovering(this)) return;
            if (!Draggable)              return;

            if (!Mouse.IsDown(MouseButton.Left) || Mouse.TimeDown(MouseButton.Left) < TimeSpan.FromSeconds(0.2f)) 
                return;
            
            Drag?.Invoke(this, new ControlMouseInputEventArgs(Mouse));

            Position += Mouse.LocalPositionDelta;
        }

        /// <summary>
        /// Returns the current position of the parent or canvas position.
        /// </summary>
        protected Vector2 GetParentActualPosition() => Parent == null ? Vector2.Zero : parent.ActualBoundingBox.Position;

        /// <summary>
        /// Returns the current size of the parent or canvas size.
        /// </summary>
        protected Vector2 GetParentActualSize() => Parent == null ? Vector2.One : parent.ActualBoundingBox.Bounds;

        /// <summary>
        /// Updates the layout of the control when anchor changes.
        /// </summary>
        protected virtual void UpdateAnchor()
        {
            var controlSize    = ActualSize;
            var parentPosition = GetParentActualPosition();
            var parentSize     = GetParentActualSize();
            
            if ((anchor & Anchor.Center) == Anchor.Center)
            {
                actualPosition = parentPosition + (parentSize * 0.5f) - (actualSize * 0.5f);

                if ((anchor & Anchor.Top) == Anchor.Top)
                    actualPosition = new Vector2(actualPosition.X, parentPosition.Y);

                if ((anchor & Anchor.Bottom) == Anchor.Bottom)
                    actualPosition = new Vector2(actualPosition.X, parentPosition.Y + parentSize.Y - controlSize.Y);

                if ((anchor & Anchor.Left) == Anchor.Left)
                    actualPosition = new Vector2(parentPosition.X, actualPosition.Y);
                
                if ((anchor & Anchor.Right) == Anchor.Right)
                    actualPosition = new Vector2(parentPosition.X + parentSize.X - controlSize.X, actualPosition.Y);
            }
            else
            {
                actualPosition = new Vector2(parentPosition.X, parentPosition.Y);
                
                if ((anchor & Anchor.Bottom) == Anchor.Bottom)
                    actualPosition = new Vector2(actualPosition.X, parentPosition.Y + parentSize.Y - controlSize.Y);
                
                if ((anchor & Anchor.Right) == Anchor.Right)
                    actualPosition = new Vector2(actualPosition.X + parentSize.X - controlSize.X, actualPosition.Y);
            }
            
            if (Positioning == Positioning.Anchor)
                position = actualPosition + parentPosition;
            else
                actualPosition += position;
        }

        /// <summary>
        /// Updates bounding box of the control when layout
        /// related properties are changed.
        /// </summary>
        protected virtual void UpdateBoundingBox()
        {
            var oldBoundingBox   = boundingBox;
            var marginTransform  = margin;
            var paddingTransform = padding;

            if (Parent != null)
            {
                var actualParentSize = GetParentActualSize();
                
                marginTransform  = UiOffset.Transform(marginTransform, actualParentSize);
                paddingTransform = UiOffset.Transform(paddingTransform, actualParentSize);
            }

            boundingBox = new Rectf(
                new Vector2(position.X - marginTransform.Right + marginTransform.Left,
                            position.Y - marginTransform.Bottom + marginTransform.Top),
                new Vector2(size.X - paddingTransform.Right + paddingTransform.Left,
                            size.Y - paddingTransform.Bottom + paddingTransform.Top));

            var oldActualBoundingBox = actualBoundingBox;

            actualBoundingBox = new Rectf(
                new Vector2(actualPosition.X - marginTransform.Right + marginTransform.Left,
                            actualPosition.Y - marginTransform.Bottom + marginTransform.Top),
                new Vector2(actualSize.X - paddingTransform.Right + paddingTransform.Left,
                            actualSize.Y - paddingTransform.Bottom + paddingTransform.Top));

            if (actualBoundingBox != oldActualBoundingBox)
                ActualBoundingBoxChanged?.Invoke(this, EventArgs.Empty);

            if (boundingBox != oldBoundingBox)
                BoundingBoxChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Updates position of the control when position related
        /// attributes are changed.
        /// </summary>
        protected virtual void UpdatePosition(bool actual = false)
        {
            switch (positioning)
            {
                case Positioning.Offset:
                case Positioning.Anchor:
                    // Anchor control based on it's anchor.
                    UpdateAnchor();
                    break;
                case Positioning.Relative:
                    // Update relative position for control.
                    var parentActualPosition = GetParentActualPosition();
                    var parentActualSize     = GetParentActualSize();

                    if (actual) position       = parentActualPosition - position;
                    else        actualPosition = position * parentActualSize + parentActualPosition;
                    break;
                case Positioning.Absolute:
                    // Update absolute position for control.
                    if (actual) position       = actualPosition;
                    else        actualPosition = position;
                    break;
                default:
                    throw new InvalidOrUnsupportedException(nameof(Positioning), Positioning);
            }
        }

        /// <summary>
        /// Updates the size of the control when size related
        /// attributes are changed.
        /// </summary>
        protected virtual void UpdateSize(bool actual = false)
        {
            var parentSize = GetParentActualSize();

            if (actual)
                size = new Vector2(actualSize.X / parentSize.X, actualSize.Y / parentSize.Y);
            else
                actualSize = size * parentSize;
        }
        
        protected virtual void InternalReceiveKeyboardInput(IGameEngineTime time, IKeyboardDevice keyboard)
        {
        }

        protected virtual void InternalReceiveMouseInput(IGameEngineTime time, IMouseDevice mouse)
        {
        }

        protected virtual void InternalReceiveTextInput(IGameEngineTime time, string text)
        {
        }

        protected virtual void InternalAfterDraw(IGraphicsFragment fragment, IGameEngineTime time)
        {
        }

        protected virtual void InternalDraw(IGraphicsFragment fragment, IGameEngineTime time)
        {
        }

        protected virtual void InternalBeforeDraw(IGraphicsFragment fragment, IGameEngineTime time)
        {
        }
        
        protected virtual void InternalUpdate(IGameEngineTime time)
        {
        }
        
        public virtual void UpdateLayout()
        {
            UpdateSize();
            
            UpdatePosition();

            UpdateBoundingBox();
        }
        
        /// <summary>
        /// Returns the destination rectangle of the control for rendering.
        /// </summary>
        protected Rectangle GetRenderDestinationRectangle()
        {
            var screenPosition = UiCanvas.ToScreenUnits(actualBoundingBox.Position);
            var screenSize     = UiCanvas.ToScreenUnits(actualBoundingBox.Bounds);
            
            return new Rectangle((int)Math.Floor(screenPosition.X),
                                 (int)Math.Floor(screenPosition.Y),
                                 (int)Math.Floor(screenSize.X),
                                 (int)Math.Floor(screenSize.Y));
        }

        public void AfterDraw(IGraphicsFragment fragment, IGameEngineTime time)
        {
            if (!Visible) return;

            InternalAfterDraw(fragment, time);
        }

        public void Draw(IGraphicsFragment fragment, IGameEngineTime time)
        {
            if (!Visible) return;

            InternalDraw(fragment, time);
        }

        public void BeforeDraw(IGraphicsFragment fragment, IGameEngineTime time)
        {
            if (!Visible) return;
            
            InternalBeforeDraw(fragment, time);
        }

        public virtual void Focus()
        {
            if (HasFocus || !VisibleFromParent)
                return;
                
            HasFocus = true;

            FocusChanged?.Invoke(this, EventArgs.Empty);
        }

        public virtual void Defocus()
        {
            if (!HasFocus)
                return;
            
            HasFocus = false;

            FocusChanged?.Invoke(this, EventArgs.Empty);
        }

        public virtual void Enable()
        {
            if (Enabled)
                return;
            
            Enabled = true;

            EnabledChanged?.Invoke(this, EventArgs.Empty);
        }

        public virtual void Disable()
        {
            if (!Enabled)
                return;
            
            // Defocus control in case when it is being disabled
            // and it contains focus.
            if (HasFocus)
                Defocus();

            Enabled = false;

            EnabledChanged?.Invoke(this, EventArgs.Empty);
        }
        
        public virtual void Show()
        {
            if (Visible)
                return;
            
            Visible = true;

            VisibilityChanged?.Invoke(this, EventArgs.Empty);
        }

        public virtual void Hide()
        {
            if (!Visible)
                return;
            
            Visible = false;

            VisibilityChanged?.Invoke(this, EventArgs.Empty);
        }

        public virtual void EnableDrag()
            => Draggable = true;

        public virtual void DisableDrag()
            => Draggable = false;

        public virtual void ResumeKeyboardInputUpdate()
        {
            if (AcceptsKeyboardInput) 
                return;

            AcceptsKeyboardInput = true;

            KeyboardInputEnabledChanged?.Invoke(this, EventArgs.Empty);
        }

        public virtual void SuspendKeyboardInputUpdate()
        {
            if (!AcceptsKeyboardInput) 
                return;

            AcceptsTextInput = false;

            KeyboardInputEnabledChanged?.Invoke(this, EventArgs.Empty);
        }

        public virtual void ResumeMouseInputUpdate()
        {
            if (AcceptsMouseInput)
                return;

            AcceptsMouseInput = true;

            MouseInputEnabledChanged?.Invoke(this, EventArgs.Empty);
        }

        public virtual void SuspendMouseInputUpdate()
        {
            if (!AcceptsMouseInput)
                return;

            AcceptsMouseInput = false;

            MouseInputEnabledChanged?.Invoke(this, EventArgs.Empty);
        }

        public virtual void ResumeTextInputUpdate()
        {
            if (AcceptsTextInput)
                return;

            AcceptsTextInput = true;

            TextInputEnabledChanged?.Invoke(this, EventArgs.Empty);
        }

        public virtual void SuspendTextInputUpdate()
        {
            if (!AcceptsTextInput)
                return;

            AcceptsTextInput = false;

            TextInputEnabledChanged?.Invoke(this, EventArgs.Empty);
        }

        public void ReceiveKeyboardInput(IGameEngineTime time, IKeyboardDevice keyboard)
        {
            if (!Enabled)
                return;

            if (!AcceptsKeyboardInput)
            {
                Keyboard.Update(time);

                return;
            }

            Keyboard.Update(time, keyboard);

            InternalReceiveKeyboardInput(time, keyboard);

            KeyboardInputReceived?.Invoke(this, new ControlKeyboardInputEventArgs(Keyboard));
        }

        public void ReceiveMouseInput(IGameEngineTime time, IMouseDevice mouse)
        {
            if (!Enabled)
                return;

            if (!AcceptsMouseInput)
            {
                Mouse.Update(time);

                UpdateMouseState();

                return;
            }

            Mouse.Update(time, mouse);

            UpdateMouseDrag();

            UpdateMouseState();

            InternalReceiveMouseInput(time, mouse);

            MouseInputReceived?.Invoke(this, new ControlMouseInputEventArgs(Mouse));
        }

        public void ReceiveTextInput(IGameEngineTime time, string text)
        {
            if (!Enabled)
                return;

            if (!AcceptsTextInput) 
                return;

            Input = text;

            InternalReceiveTextInput(time, text);

            TextInputReceived?.Invoke(this, new ControlTextInputEventArgs(Input));
        }

        public void Update(IGameEngineTime time)
        {
            if (!VisibleFromParent && HasFocus)
                Defocus();
            
            if (!Enabled)
                return;

            InternalUpdate(time);
        }
    }
}