using System.Linq;
using Fracture.Common.Di.Attributes;
using Fracture.Engine.Core;
using Fracture.Engine.Input.Devices;
using Microsoft.Xna.Framework.Input;

namespace Fracture.Engine.Input
{
    public interface IKeyboardInputSystem : IGameEngineSystem, IInputBinder<Keys>
    {
        #region Properties
        IKeyboardDevice Device
        {
            get;
        }
        #endregion
    }
    
    public sealed class KeyboardInputSystem : ActiveGameEngineSystem, IKeyboardInputSystem
    {
        #region Fields
        private readonly KeyboardInputManager manager;
        #endregion

        #region Properties
        public IKeyboardDevice Device
        {
            get;
            private set;
        }
        #endregion

        [BindingConstructor]
        public KeyboardInputSystem(IGameEngine engine, IInputDeviceSystem input, int priority)
            : base(engine, priority)
        {
            // Get mouse device.
            Device = input.First(d => d is IKeyboardDevice) as IKeyboardDevice;

            // Initialize manager.
            manager = new KeyboardInputManager(Device);
        }

        public override void Update()
            => manager.Update(Engine.Time);
        
        public void Bind(string name, InputBindingCallback callback, InputTriggerState state, params Keys[] combination)
            => manager.Bind(name, callback, state, combination);

        public void Bind(string name, InputBindingCallback callback, InputTriggerState state, Keys trigger)
            => manager.Bind(name, callback, state, trigger);

        public void Clear()
            => manager.Clear();

        public void Rebind(string name, InputTriggerState state, params Keys[] combination)
            => manager.Rebind(name, state, combination);

        public void Rebind(string name, InputTriggerState state, Keys trigger)
            => manager.Rebind(name, state, trigger);

        public void Unbind(string name)
            => manager.Unbind(name);
    }
}
