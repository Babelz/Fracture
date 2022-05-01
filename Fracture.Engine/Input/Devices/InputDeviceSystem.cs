using System;
using System.Collections;
using System.Collections.Generic;
using Fracture.Common.Di.Attributes;
using Fracture.Common.Di.Binding;
using Fracture.Engine.Core;
using Fracture.Engine.Core.Systems;
using NLog;

namespace Fracture.Engine.Input.Devices
{
    /// <summary>
    /// Interface for implementing input device interfaces. Input devices
    /// are used to read input from the user.
    /// </summary>
    public interface IInputDevice 
    {
        #region Properties
        /// <summary>
        /// How many states of the device are being stored. 
        /// </summary>
        int StatesCount
        {
            get;
        }
        #endregion

        /// <summary>
        /// Polls the input device and gets its current state.
        /// </summary>
        void Poll(IGameEngineTime time);
    }
    
    /// <summary>
    /// Interface for implementing input device systems.
    /// </summary>
    public interface IInputDeviceSystem : IEnumerable<IInputDevice>
    {
        // Nothing to implement. Union interface.
    }
    
    public sealed class InputDeviceSystem : GameEngineSystem, IInputDeviceSystem
    {
        #region Static fields
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion
        
        #region Fields
        private readonly HashSet<IInputDevice> devices;
        #endregion

        [BindingConstructor]
        public InputDeviceSystem(IGameObjectActivatorSystem activator, IEnumerable<IPlatformDeviceConfiguration> configurations)
        {
            devices = new HashSet<IInputDevice>();
            
            foreach (var configuration in configurations)
            {
                try
                {
                    var device = configuration.CreateDevice(activator);

                    if (!devices.Add(device))
                        throw new InvalidOperationException($"duplicated input device {device.GetType().FullName} in configuration")
                        {
                            Data =
                            {
                                { nameof(device), device },
                                { nameof(devices), devices }
                            }
                        };   
                }
                catch (Exception e)
                {
                    Log.Warn(e, "input device activation failed");
                    
                    throw;
                }
            }
        }

        public override void Update(IGameEngineTime time)
        {
            foreach (var device in devices)
                device.Poll(time);
        }

        public void Register(IInputDevice device)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));

            if (!devices.Add(device))
                throw new InvalidOperationException($"could not register device {device}");
        }
        public void Unregister(IInputDevice device)
        {
            if (!devices.Remove(device))
                throw new InvalidOperationException($"could not remove device {device}");
        }

        public IEnumerator<IInputDevice> GetEnumerator()
            => devices.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}
