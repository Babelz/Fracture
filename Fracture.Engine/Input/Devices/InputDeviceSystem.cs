﻿using System;
using System.Collections;
using System.Collections.Generic;
using Fracture.Common.Di.Attributes;
using Fracture.Engine.Core;

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
    /// 
    /// TODO: update to support multiple keyboards, mouses and game pads.
    /// </summary>
    public interface IInputDeviceSystem : IGameEngineSystem, IEnumerable<IInputDevice>
    {
        void Register(IInputDevice device);
        
        void Unregister(IInputDevice device);
    }
    
    public sealed class InputDeviceSystem : GameEngineSystem, IInputDeviceSystem
    {
        #region Fields
        private readonly HashSet<IInputDevice> devices;
        #endregion

        public InputDeviceSystem(params IInputDevice[] devices)
            => this.devices = new HashSet<IInputDevice>(devices);
        
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
