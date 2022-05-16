using System;
using Fracture.Engine.Core;

namespace Fracture.Engine.Input
{
    /// <summary>
    /// Enumeration that defines all supported trigger states.
    /// </summary>
    public enum InputTriggerState : byte
    {
        /// <summary>
        /// Trigger is released, button can be in this state for period of
        /// one frame. State prior to released is down or pressed.
        /// </summary>
        Released = 0,

        /// <summary>
        /// Trigger is not being pressed. State prior to up is
        /// pressed or released.
        /// </summary>
        Up,

        /// <summary>
        /// Trigger is pressed down, can be in this state for period
        /// of one frame. State prior to pressed is up.
        /// </summary>
        Pressed,

        /// <summary>
        /// Trigger is being held down. State prior to down is pressed.
        /// </summary>
        Down
    }

    public delegate void InputBindingCallback(IGameEngineTime time);

    public sealed class InputBinding<T> where T : struct
    {
        #region Properties
        public string Name
        {
            get;
        }

        public InputBindingCallback Callback
        {
            get;
        }

        public T [] Triggers
        {
            get;
            set;
        }

        public InputTriggerState State
        {
            get;
            set;
        }
        #endregion

        public InputBinding(string name, T [] triggers, InputBindingCallback callback, InputTriggerState state)
        {
            Name     = !string.IsNullOrEmpty(name) ? name : throw new ArgumentNullException(nameof(name));
            Callback = callback ?? throw new ArgumentNullException(nameof(callback));
            Triggers = triggers;
            State    = state;
        }
    }
}