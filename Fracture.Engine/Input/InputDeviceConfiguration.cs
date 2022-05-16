using System;
using Fracture.Common.Di.Binding;
using Fracture.Engine.Core.Systems;
using Fracture.Engine.Input.Devices;

namespace Fracture.Engine.Input
{
    public interface IPlatformDeviceConfiguration
    {
        IInputDevice CreateDevice(IGameObjectActivatorSystem activator);
    }

    public sealed class DesktopKeyboardConfiguration : IPlatformDeviceConfiguration
    {
        #region Fields
        private readonly int statesCount;
        #endregion

        public DesktopKeyboardConfiguration(int statesCount)
        {
            if (statesCount < 0)
                throw new ArgumentOutOfRangeException(nameof(statesCount), "expecting positive value");

            this.statesCount = statesCount;
        }

        public IInputDevice CreateDevice(IGameObjectActivatorSystem activator) =>
            activator.Activate<KeyboardDevice>(BindingValue.Const(nameof(statesCount), statesCount));
    }

    public sealed class DesktopMouseConfiguration : IPlatformDeviceConfiguration
    {
        #region Fields
        private readonly int statesCount;
        #endregion

        public DesktopMouseConfiguration(int statesCount)
        {
            if (statesCount < 0)
                throw new ArgumentOutOfRangeException(nameof(statesCount), "expecting positive value");

            this.statesCount = statesCount;
        }

        public IInputDevice CreateDevice(IGameObjectActivatorSystem activator) =>
            activator.Activate<MouseDevice>(BindingValue.Const(nameof(statesCount), statesCount));
    }
}