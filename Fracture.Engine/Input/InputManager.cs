using System;
using System.Collections.Generic;
using System.Linq;
using Fracture.Engine.Core;
using Fracture.Engine.Input.Devices;
using Microsoft.Xna.Framework.Input;

namespace Fracture.Engine.Input
{
    /// <summary>
    /// Interface for creating input binders. Binders manage user defined bindings for input 
    /// rerouting.
    /// </summary>
    public interface IInputBinder<in T> where T : struct
    {
        void Bind(string name, InputBindingCallback callback, InputTriggerState state, params T[] combination);
        void Bind(string name, InputBindingCallback callback, InputTriggerState state, T trigger);

        void Rebind(string name, InputTriggerState state, params T[] combination);
        void Rebind(string name, InputTriggerState state, T trigger);
        void Unbind(string name);

        void Clear();
    }

    /// <summary>
    /// Interface for implementing input managers. Managers provide input binding updates
    /// and input binding support.
    /// </summary>
    public interface IInputManager<in T> : IInputBinder<T> where T : struct
    {
        #region Properties
        int CombinationAccuracy
        {
            get;
        }
        #endregion

        void Update(IGameEngineTime time);
    }
    
    public abstract class InputManager<T> : IInputManager<T> where T : struct
    {
        #region Constant fields
        public const int DefaultCombinationAccuracy = 4;
        #endregion

        #region Fields
        // TODO: fak.
        private readonly List<InputBinding<T>> bindings;
        #endregion

        #region Properties
        public int CombinationAccuracy
        {
            get;
        }
        #endregion

        protected InputManager(int combinationAccuracy = DefaultCombinationAccuracy)
        {
            if (combinationAccuracy < 0)
                throw new ArgumentOutOfRangeException(nameof(combinationAccuracy), $"{nameof(combinationAccuracy)} must be greater or equal to 1");

            CombinationAccuracy = combinationAccuracy;

            bindings = new List<InputBinding<T>>();
        }

        protected abstract bool IsTriggerUp(T[] triggers);
        protected abstract bool IsTriggerReleased(T[] triggers);
        protected abstract bool IsTriggerDown(T[] triggers);
        protected abstract bool IsTriggerPressed(T[] triggers);

        protected bool TestTriggers(T[] triggers, Func<int, T, bool> test)
        {
            if (triggers.Length == 1)
                return test(0, triggers[0]);

            var result = false;

            for (var i = 0; i < triggers.Length; i++)
            {
                for (var j = 0; j < CombinationAccuracy; j++)
                {
                    result = test(j, triggers[i]);

                    if (result) break;
                }

                if (!result)
                    return false;
            }

            return true;
        }

        public void Bind(string name, InputBindingCallback callback, InputTriggerState state, T trigger)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            if (callback == null)           throw new ArgumentNullException(nameof(callback));

            bindings.Add(new InputBinding<T>(name, new[] { trigger }, callback, state));
        }

        public void Bind(string name, InputBindingCallback callback, InputTriggerState state, params T[] combination)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            if (callback == null)           throw new ArgumentNullException(nameof(callback));
            if (combination.Length < 2)     throw new ArgumentOutOfRangeException(nameof(combination), "atleast 2 triggers are required for combination");

            bindings.Add(new InputBinding<T>(name, combination, callback, state));
        }

        public void Clear()
            => bindings.Clear();

        public void Rebind(string name, InputTriggerState state, T trigger)
        {
            var binding = bindings.First(b => b.Name == name);
            
            if (binding.Triggers.Length > 1)
                binding.Triggers = new[] { trigger };
            else
                binding.Triggers[0] = trigger;

            binding.State = state;
        }

        public void Rebind(string name, InputTriggerState state, params T[] combination)
        {
            if (combination.Length < 2)
                throw new ArgumentOutOfRangeException(nameof(combination), "at least 2 triggers are required for combination");
            
            var binding = bindings.First(b => b.Name == name);
            
            binding.Triggers = combination.ToArray();
            binding.State    = state;
        }

        public void Unbind(string name)
            => bindings.Remove(bindings.First(b => b.Name == name));
            
        public void Update(IGameEngineTime time)
        {
            for (var i = 0; i < bindings.Count; i++)
            {
                var binding = bindings[i];

                var triggered = binding.State switch
                {
                    InputTriggerState.Released => IsTriggerReleased(binding.Triggers),
                    InputTriggerState.Up       => IsTriggerUp(binding.Triggers),
                    InputTriggerState.Pressed  => IsTriggerPressed(binding.Triggers),
                    InputTriggerState.Down     => IsTriggerDown(binding.Triggers),
                    _                          => false
                };

                if (triggered) 
                    binding.Callback(time);
            }
        }
    }
    
    public sealed class KeyboardInputManager : InputManager<Keys>
    {
        #region Fields
        private readonly IKeyboardDevice device;
        #endregion

        public KeyboardInputManager(IKeyboardDevice device, int combinationAccuracy = DefaultCombinationAccuracy)
            : base(combinationAccuracy) => this.device = device ?? throw new ArgumentNullException(nameof(device));

        protected override bool IsTriggerDown(Keys[] triggers)
            => TestTriggers(triggers, (i, k) => device.IsKeyDown(k, i));

        protected override bool IsTriggerPressed(Keys[] triggers)
            => TestTriggers(triggers, (i, k) => device.IsKeyPressed(k, i));

        protected override bool IsTriggerReleased(Keys[] triggers)
            => TestTriggers(triggers, (i, k) => device.IsKeyReleased(k, i));

        protected override bool IsTriggerUp(Keys[] triggers)
            => TestTriggers(triggers, (i, k) => device.IsKeyUp(k, i));
    }
    
    public sealed class MouseInputManager : InputManager<MouseButton>
    {
        #region Fields
        private readonly IMouseDevice device;
        #endregion

        public MouseInputManager(IMouseDevice device, int combinationAccuracy = DefaultCombinationAccuracy)
            : base(combinationAccuracy) => this.device = device ?? throw new ArgumentNullException(nameof(device));

        protected override bool IsTriggerDown(MouseButton[] triggers)
            => TestTriggers(triggers, (i, k) => device.IsButtonDown(k, i));

        protected override bool IsTriggerPressed(MouseButton[] triggers)
            => TestTriggers(triggers, (i, k) => device.IsButtonPressed(k, i));

        protected override bool IsTriggerReleased(MouseButton[] triggers)
            => TestTriggers(triggers, (i, k) => device.IsButtonReleased(k, i));

        protected override bool IsTriggerUp(MouseButton[] triggers)
            => TestTriggers(triggers, (i, k) => device.IsButtonUp(k, i));
    }
}
