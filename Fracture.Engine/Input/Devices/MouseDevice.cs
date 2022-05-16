using System;
using System.Collections.Generic;
using System.Linq;
using Fracture.Common.Collections;
using Fracture.Common.Di.Attributes;
using Fracture.Engine.Core;
using Fracture.Engine.Core.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Fracture.Engine.Input.Devices
{
    /// <summary>
    /// Enumeration containing all supported mouse buttons.
    /// </summary>
    [Flags]
    public enum MouseButton : byte
    {
        None = 0,
        Left = (1 << 0),
        Middle = (1 << 1),
        Right = (1 << 2),
        X1 = (1 << 3),
        X2 = (1 << 4)
    }

    /// <summary>
    /// Structure that represents the state of the mouse.
    /// </summary>
    public struct MouseState
    {
        #region Properties
        /// <summary>
        /// Returns the position of the mouse.
        /// </summary>
        public Point Position
        {
            get;
            private set;
        }

        /// <summary>
        /// Flags containing buttons pressed.
        /// </summary>
        public MouseButton ButtonsDown
        {
            get;
        }

        /// <summary>
        /// Returns the value of the scroll wheel.
        /// </summary>
        public int ScrollWheelValue
        {
            get;
        }
        #endregion

        public MouseState(Point position, MouseButton pressedButtons, int scrollWheelValue)
        {
            Position         = position;
            ButtonsDown      = pressedButtons;
            ScrollWheelValue = scrollWheelValue;
        }

        public void Transform(Point transform) => Position += transform;
    }

    /// <summary>
    /// Interface for implementing mouse device interfaces.
    /// </summary>
    public interface IMouseDevice : IInputDevice
    {
        bool IsButtonDown(MouseButton button, int frame = 0);
        bool IsButtonPressed(MouseButton button, int frame = 0);
        bool IsButtonUp(MouseButton button, int frame = 0);
        bool IsButtonReleased(MouseButton button, int frame = 0);

        IEnumerable<MouseButton> GetButtonsDown(int frame = 0);
        IEnumerable<MouseButton> GetButtonsPressed(int frame = 0);
        IEnumerable<MouseButton> GetButtonsUp(int frame = 0);
        IEnumerable<MouseButton> GetButtonsReleased(int frame = 0);

        Point GetPosition(int frame = 0);
        int GetScrollWheelValue(int frame = 0);
        Rectangle GetRectangle(int frame = 0);

        TimeSpan GetButtonTimeDown(MouseButton button);
        TimeSpan GetButtonTimeUp(MouseButton button);
    }

    /// <summary>
    /// Default implementation of <see cref="IMouseDevice"/> that uses MonoGame to toll mouse states.
    /// </summary>
    public sealed class MouseDevice : IMouseDevice
    {
        #region Static fields
        private static readonly MouseButton [] MouseButtonValues = typeof(MouseButton).GetEnumValues().Cast<MouseButton>().ToArray();
        #endregion

        #region Fields
        private readonly StateWatcher<MouseButton> buttonWatcher;

        private readonly CircularBuffer<MouseState> mouseStateBuffer;
        #endregion

        #region Properties
        public int StatesCount
        {
            get;
        }
        #endregion

        /// <summary>
        /// Creates new instance of <see cref="MouseDevice"/> with given states count, mouse always attempts
        /// to ensure that there are at least 2 states being recorded.
        /// </summary>
        /// <param name="statesCount"></param>
        [BindingConstructor]
        public MouseDevice(int statesCount = 0)
        {
            statesCount += 2;

            if (statesCount < 2)
                throw new ArgumentOutOfRangeException(nameof(statesCount));

            StatesCount = statesCount;

            buttonWatcher    = new StateWatcher<MouseButton>();
            mouseStateBuffer = new CircularBuffer<MouseState>(StatesCount);
        }

        public IEnumerable<MouseButton> GetButtonsDown(int frame = 0)
        {
            if (frame >= StatesCount)
                throw new ArgumentOutOfRangeException(nameof(frame), $"{nameof(frame)} >= {StatesCount}");

            IEnumerable<MouseButton> GetEnumeration()
            {
                for (var i = 0; i < MouseButtonValues.Length; i++)
                {
                    if (IsButtonDown(MouseButtonValues[i]))
                        yield return MouseButtonValues[i];
                }
            }

            return GetEnumeration();
        }

        public IEnumerable<MouseButton> GetButtonsPressed(int frame = 0)
        {
            if (frame >= StatesCount)
                throw new ArgumentOutOfRangeException(nameof(frame), $"{nameof(frame)} >= {StatesCount}");

            IEnumerable<MouseButton> GetEnumeration()
            {
                for (var i = 0; i < MouseButtonValues.Length; i++)
                {
                    if (IsButtonPressed(MouseButtonValues[i]))
                        yield return MouseButtonValues[i];
                }
            }

            return GetEnumeration();
        }

        public IEnumerable<MouseButton> GetButtonsUp(int frame = 0)
        {
            if (frame >= StatesCount)
                throw new ArgumentOutOfRangeException(nameof(frame), $"{nameof(frame)} >= {StatesCount}");

            IEnumerable<MouseButton> GetEnumeration()
            {
                for (var i = 0; i < MouseButtonValues.Length; i++)
                {
                    if (IsButtonUp(MouseButtonValues[i]))
                        yield return MouseButtonValues[i];
                }
            }

            return GetEnumeration();
        }

        public IEnumerable<MouseButton> GetButtonsReleased(int frame = 0)
        {
            if (frame >= StatesCount)
                throw new ArgumentOutOfRangeException(nameof(frame), $"{nameof(frame)} >= {StatesCount}");

            IEnumerable<MouseButton> GetEnumeration()
            {
                for (var i = 0; i < MouseButtonValues.Length; i++)
                {
                    if (IsButtonReleased(MouseButtonValues[i]))
                        yield return MouseButtonValues[i];
                }
            }

            return GetEnumeration();
        }

        public Point GetPosition(int frame = 0)
        {
            if (frame >= StatesCount)
                throw new ArgumentOutOfRangeException(nameof(frame), $"{nameof(frame)} >= {StatesCount}");

            return mouseStateBuffer.AtOffset(-frame).Position;
        }

        public Rectangle GetRectangle(int frame = 0)
        {
            var position = GetPosition(frame);

            return new Rectangle(position.X,
                                 position.Y,
                                 1,
                                 1);
        }

        public int GetScrollWheelValue(int frame = 0)
        {
            if (frame >= StatesCount)
                throw new ArgumentOutOfRangeException(nameof(frame), $"{nameof(frame)} >= {StatesCount}");

            return mouseStateBuffer.AtOffset(-frame).ScrollWheelValue;
        }

        public bool IsButtonDown(MouseButton button, int frame = 0)
        {
            if (frame >= StatesCount)
                throw new ArgumentOutOfRangeException(nameof(frame), $"{nameof(frame)} >= {StatesCount}");

            return (mouseStateBuffer.AtOffset(-frame).ButtonsDown & button) == button;
        }

        public bool IsButtonPressed(MouseButton button, int frame = 0)
        {
            if (frame >= StatesCount)
                throw new ArgumentOutOfRangeException(nameof(frame), $"{nameof(frame)} >= {StatesCount}");

            return (mouseStateBuffer.AtOffset(-frame).ButtonsDown & button) == button &&
                   (mouseStateBuffer.AtOffset(-frame - 1).ButtonsDown & button) != button;
        }

        public bool IsButtonUp(MouseButton button, int frame = 0)
        {
            if (frame >= StatesCount)
                throw new ArgumentOutOfRangeException(nameof(frame), $"{nameof(frame)} >= {StatesCount}");

            return (mouseStateBuffer.AtOffset(-frame).ButtonsDown & button) != button;
        }

        public bool IsButtonReleased(MouseButton button, int frame = 0)
        {
            if (frame >= StatesCount)
                throw new ArgumentOutOfRangeException(nameof(frame), $"{nameof(frame)} >= {StatesCount}");

            var buttonsDown = mouseStateBuffer.AtOffset(-frame - 1).ButtonsDown;
            var buttonsUp   = mouseStateBuffer.AtOffset(-frame).ButtonsDown;

            return (buttonsDown & button) == button && (buttonsUp & button) != button;
        }

        public TimeSpan GetButtonTimeDown(MouseButton button) => buttonWatcher.TimeActive(button);

        public TimeSpan GetButtonTimeUp(MouseButton button) => buttonWatcher.TimeInactive(button);

        public void Poll(IGameEngineTime time)
        {
            // Poll MonoGame mouse state.
            var state   = Mouse.GetState();
            var buttons = MouseButton.None;

            // Determine button states.
            if (state.LeftButton == ButtonState.Pressed) buttons   |= MouseButton.Left;
            if (state.MiddleButton == ButtonState.Pressed) buttons |= MouseButton.Middle;
            if (state.RightButton == ButtonState.Pressed) buttons  |= MouseButton.Right;
            if (state.XButton1 == ButtonState.Pressed) buttons     |= MouseButton.X1;
            if (state.XButton2 == ButtonState.Pressed) buttons     |= MouseButton.X2;

            // Create new state.
            mouseStateBuffer.Push(new MouseState(new Point(state.X, state.Y), buttons, state.ScrollWheelValue));

            buttonWatcher.Update(time, GetButtonsUp(), GetButtonsDown());
        }
    }
}