using System;
using System.Collections.Generic;
using System.Linq;
using Fracture.Engine.Core;
using Fracture.Engine.Core.Components;
using Fracture.Engine.Core.Primitives;
using Fracture.Engine.Input.Devices;
using Fracture.Engine.Ui.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Fracture.Engine.Ui.Components
{
    public abstract class ControlInputManager<TTrigger, TDevice>
    {
        #region Fields
        private readonly StateWatcher<TTrigger> watcher;
        #endregion

        #region Properties
        public abstract IEnumerable<TTrigger> Pressed
        {
            get;
            protected set;
        }
        public abstract IEnumerable<TTrigger> Down
        {
            get;
            protected set;
        }
        public abstract IEnumerable<TTrigger> Released
        {
            get;
            protected set;
        }
        public abstract IEnumerable<TTrigger> Up
        {
            get;
            protected set;
        }
        #endregion
        
        protected ControlInputManager()
            => watcher = new StateWatcher<TTrigger>();
        
        public virtual void Update(IGameEngineTime time, TDevice device)
            => watcher.Update(time, Up, Down);

        public void Update(IGameEngineTime time)
        {
            Up       = Enumerable.Empty<TTrigger>();
            Down     = Enumerable.Empty<TTrigger>();
            Released = Enumerable.Empty<TTrigger>();
            Pressed  = Enumerable.Empty<TTrigger>();

            watcher.Update(time, Up, Down);
        }
        
        public bool IsReleased(TTrigger trigger)
            => Released.Contains(trigger);

        public bool IsDown(TTrigger trigger)
            => Down.Contains(trigger);

        public bool IsPressed(TTrigger trigger)
            => Pressed.Contains(trigger);

        public bool IsUp(TTrigger trigger)
            => Up.Contains(trigger);

        public TimeSpan TimeDown(TTrigger trigger)
            => watcher.TimeActive(trigger);

        public TimeSpan TimeUp(TTrigger trigger)
            => watcher.TimeInactive(trigger);
    }
    
    /// <summary>
    /// Input manager for controls that handle mouse input. Handles button states
    /// and mouse positions.
    /// /// </summary>
    public sealed class ControlMouseInputManager : ControlInputManager<MouseButton, IMouseDevice>
    {
        #region Properties
        /// <summary>
        /// Returns the current position in screen units.
        /// </summary>
        public Vector2 CurrentScreenPosition => UiCanvas.ToScreenUnits(CurrentLocalPosition);

        /// <summary>
        /// Returns the last position in screen units.
        /// </summary>
        public Vector2 LastScreenPosition => UiCanvas.ToScreenUnits(LastLocalPosition);

        public Vector2 ScreenPositionDelta => CurrentScreenPosition - LastScreenPosition;
        
        /// <summary>
        /// Current position in local units.
        /// </summary>
        public Vector2 CurrentLocalPosition
        {
            get;
            private set;
        }

        /// <summary>
        /// Last position in local units.
        /// </summary>
        public Vector2 LastLocalPosition
        {
            get;
            private set;
        }
        
        public Vector2 LocalPositionDelta => CurrentLocalPosition - LastLocalPosition;

        public bool PositionChanged => CurrentLocalPosition != LastLocalPosition;

        public int CurrentScrollValue
        {
            get;
            private set;
        }

        public int LastScrollValue
        {
            get;
            private set;
        }
        
        public int ScrollDelta => CurrentScrollValue - LastScrollValue;

        public bool ScrollValueChanged => CurrentScrollValue != LastScrollValue;

        public override IEnumerable<MouseButton> Pressed
        {
            get;
            protected set;
        }
        public override IEnumerable<MouseButton> Down
        {
            get;
            protected set;
        }
        public override IEnumerable<MouseButton> Released
        {
            get;
            protected set;
        }
        public override IEnumerable<MouseButton> Up
        {
            get;
            protected set;
        }
        #endregion

        public ControlMouseInputManager()
        {
        }

        public override void Update(IGameEngineTime time, IMouseDevice device)
        {
            Pressed  = device.GetButtonsPressed();
            Down     = device.GetButtonsDown();
            Released = device.GetButtonsReleased();
            Up       = device.GetButtonsUp();

            base.Update(time, device);

            CurrentLocalPosition = UiCanvas.ToLocalUnits(device.GetPosition().ToVector2());
            LastLocalPosition    = UiCanvas.ToLocalUnits(device.GetPosition(1).ToVector2());

            CurrentScrollValue = device.GetScrollWheelValue();
            LastScrollValue    = device.GetScrollWheelValue(1);
        }

        public Vector2 Transform(IControl parent)
        {
            if (parent is IStaticContainerControl container && container.UseRenderTarget)
            {
                // If the container is using render targets, we must apply view offset
                // to the mouse states position as transformation. This transformation
                // must be inverse relative to the parent as the parent control does
                // normal transformation.
                return UiCanvas.ToLocalUnits(CurrentLocalPosition.X - container.ViewOffset.X,
                                             CurrentLocalPosition.Y - container.ViewOffset.Y);
            }

            return CurrentLocalPosition;
        }

        public bool IsHovering(Rectangle area)
            => area.Intersects(new Rectangle(
                (int)Math.Floor(UiCanvas.ToHorizontalScreenUnits(CurrentLocalPosition.X)), 
                (int)Math.Floor(UiCanvas.ToVerticalScreenUnits(CurrentLocalPosition.Y)), 
                1,
                1));

        public bool IsHovering(Rectf area)
            => Rectf.Intersects(area, new Rectf(CurrentLocalPosition, UiCanvas.ToLocalUnits(Vector2.One)));

        public bool IsHovering(IControl control)
            => IsHovering(control.CollisionBoundingBox);    
    }
        
    public sealed class ControlKeyboardInputManager : ControlInputManager<Keys, IKeyboardDevice>
    {
        #region Properties
        public override IEnumerable<Keys> Pressed
        {
            get;
            protected set;
        }
        public override IEnumerable<Keys> Down
        {
            get;
            protected set;
        }
        public override IEnumerable<Keys> Released
        {
            get;
            protected set;
        }
        public override IEnumerable<Keys> Up
        {
            get;
            protected set;
        }
        #endregion

        public ControlKeyboardInputManager()
        {
        }

        public override void Update(IGameEngineTime time, IKeyboardDevice device)
        {
            Pressed  = device.GetKeysPressed();
            Down     = device.GetKeysDown();
            Released = device.GetKeysReleased();
            Up       = device.GetKeysUp();

            base.Update(time, device);
        }
    }
}
