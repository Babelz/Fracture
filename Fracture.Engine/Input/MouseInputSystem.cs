﻿using System.Linq;
using Fracture.Common.Di.Attributes;
using Fracture.Engine.Core;
using Fracture.Engine.Input.Devices;

namespace Fracture.Engine.Input
{
    public interface IMouseInputSystem : IGameEngineSystem, IInputBinder<MouseButton>
    {
        #region Properties
        IMouseDevice Device
        {
            get;
        }
        #endregion
    }
    
    public sealed class MouseInputSystem : ActiveGameEngineSystem, IMouseInputSystem
    {
        #region Fields
        private readonly MouseInputManager manager;
        #endregion

        #region Properties
        public IMouseDevice Device
        {
            get;
            private set;
        }
        #endregion

        [BindingConstructor]
        public MouseInputSystem(IGameEngine engine, IInputDeviceSystem input, int priority) 
            : base(engine, priority)
        {
            // Get mouse device.
            Device = input.First(d => d is IMouseDevice) as IMouseDevice;

            // Initialize manager.
            manager = new MouseInputManager(Device);
        }

        public override void Update()
            => manager.Update(Engine.Time);
        
        public void Bind(string name, InputBindingCallback callback, InputTriggerState state, params MouseButton[] combination)
            => manager.Bind(name, callback, state, combination);

        public void Bind(string name, InputBindingCallback callback, InputTriggerState state, MouseButton trigger)
            => manager.Bind(name, callback, state, trigger);

        public void Clear()
            => manager.Clear();

        public void Rebind(string name, InputTriggerState state, params MouseButton[] combination)
            => manager.Rebind(name, state, combination);

        public void Rebind(string name, InputTriggerState state, MouseButton trigger)
            => manager.Rebind(name, state, trigger);

        public void Unbind(string name)
            => manager.Unbind(name);
    }
}
