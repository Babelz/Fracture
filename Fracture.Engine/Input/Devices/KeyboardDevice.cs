using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fracture.Common.Collections;
using Fracture.Common.Di.Attributes;
using Fracture.Engine.Core;
using Fracture.Engine.Core.Components;
using Fracture.Engine.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Fracture.Engine.Input.Devices
{
    /// <summary>
    /// Interface for implementing keyboard device interfaces. Supports
    /// character and key state reading. Use the frames argument
    /// to read past frame states and 0 value to read the current frames
    /// state.
    /// </summary>
    public interface IKeyboardDevice : IInputDevice
    {
        /// <summary>
        /// Returns boolean declaring whether the key was pressed this frame.
        /// </summary>
        bool IsKeyPressed(Keys key, int frame = 0);

        /// <summary>
        /// Returns boolean declaring whether the key was down this frame.
        /// </summary>
        bool IsKeyDown(Keys key, int frame = 0);

        /// <summary>
        /// Returns boolean declaring whether the key is up this frame.
        /// </summary>
        bool IsKeyUp(Keys key, int frame = 0);

        /// <summary>
        /// Returns boolean declaring whether the key was released this frame.
        /// </summary>
        bool IsKeyReleased(Keys key, int frame = 0);

        IEnumerable<Keys> GetKeysDown(int frame = 0);
        IEnumerable<Keys> GetKeysPressed(int frame = 0);
        IEnumerable<Keys> GetKeysUp(int frame = 0);
        IEnumerable<Keys> GetKeysReleased(int frame = 0);

        string GetCharacters(int frame = 0);
        bool IsCapsLockToggled(int frame = 0);

        TimeSpan GetKeyTimeDown(Keys key);
        TimeSpan GetKeyTimeUp(Keys key);
    }

    /// <summary>
    /// Default implementation of <see cref="IKeyboardDevice"/> that uses MonoGame to poll key states
    /// and native functions to listen for text input.
    /// </summary>
    public sealed class KeyboardDevice : IKeyboardDevice
    {
        #region Static fields
        private static readonly HashSet<Keys> KeysHashSet
            = new HashSet<Keys>(typeof(Keys).GetEnumValues().Cast<Keys>());
        #endregion

        #region Fields
        private readonly StateWatcher<Keys> keyWatcher;

        private readonly CircularBuffer<KeyboardState> keyboardStateBuffer;
        private readonly CircularBuffer<string>        keyboardCharacterBuffer;

        private readonly StringBuilder keyboardCharacterFrameBuffer;
        #endregion

        #region Properties
        public int StatesCount
        {
            get;
        }
        #endregion

        /// <summary>
        /// Creates new instance of <see cref="KeyboardDevice"/> with given states count, keyboard always attempts
        /// to ensure that there are at least 2 states being recorded.
        /// </summary>
        [BindingConstructor]
        public KeyboardDevice(IGraphicsDeviceSystem graphics, int statesCount = 0)
        {
            // Attempt to ensure that at least 2 states get recorded.
            statesCount += 2;

            if (statesCount < 2)
                throw new ArgumentOutOfRangeException(nameof(statesCount));

            StatesCount = statesCount;

            keyWatcher              = new StateWatcher<Keys>();
            keyboardStateBuffer     = new CircularBuffer<KeyboardState>(StatesCount);
            keyboardCharacterBuffer = new CircularBuffer<string>(StatesCount);

            keyboardCharacterFrameBuffer = new StringBuilder();

            graphics.Window.TextInput += Window_TextInput;
        }

        #region Event handlers
        private void Window_TextInput(object sender, TextInputEventArgs e)
        {
            if (char.IsControl(e.Character)) return;

            keyboardCharacterFrameBuffer.Append(e.Character);
        }
        #endregion

        public IEnumerable<Keys> GetKeysDown(int frame = 0)
        {
            if (frame >= StatesCount)
                throw new ArgumentOutOfRangeException(nameof(frame), $"{nameof(frame)} >= {StatesCount}");

            return keyboardStateBuffer.AtOffset(-frame)
                                      .GetPressedKeys();
        }

        public string GetCharacters(int frame = 0)
        {
            if (frame >= StatesCount)
                throw new ArgumentOutOfRangeException(nameof(frame), $"{nameof(frame)} >= {StatesCount}");

            return keyboardCharacterBuffer.AtOffset(-frame);
        }

        public IEnumerable<Keys> GetKeysPressed(int frame = 0)
        {
            if (frame >= StatesCount)
                throw new ArgumentOutOfRangeException(nameof(frame), $"{nameof(frame)} >= {StatesCount}");

            var upKeys   = keyboardStateBuffer.AtOffset(-frame - 1);
            var downKeys = keyboardStateBuffer.AtOffset(-frame);

            return downKeys.GetPressedKeys().Where(k => upKeys.IsKeyUp(k));
        }

        public IEnumerable<Keys> GetKeysUp(int frame = 0)
        {
            if (frame >= StatesCount)
                throw new ArgumentOutOfRangeException(nameof(frame), $"{nameof(frame)} >= {StatesCount}");

            return keyboardStateBuffer.AtOffset(-frame)
                                      .GetPressedKeys()
                                      .Where(k => !KeysHashSet.Contains(k));
        }

        public IEnumerable<Keys> GetKeysReleased(int frame = 0)
        {
            if (frame >= StatesCount)
                throw new ArgumentOutOfRangeException(nameof(frame), $"{nameof(frame)} >= {StatesCount}");

            var downKeys = keyboardStateBuffer.AtOffset(-frame - 1);
            var upKeys   = keyboardStateBuffer.AtOffset(-frame);

            return upKeys.GetPressedKeys().Union(downKeys.GetPressedKeys()).Distinct();
        }

        public bool IsKeyDown(Keys key, int frame = 0)
        {
            if (frame >= StatesCount)
                throw new ArgumentOutOfRangeException(nameof(frame), $"{nameof(frame)} >= {StatesCount}");

            return keyboardStateBuffer.AtOffset(-frame).IsKeyDown(key);
        }

        public bool IsKeyPressed(Keys key, int frame = 0)
        {
            if (frame >= StatesCount)
                throw new ArgumentOutOfRangeException(nameof(frame), $"{nameof(frame)} >= {StatesCount}");

            return keyboardStateBuffer.AtOffset(-frame).IsKeyDown(key) &&
                   keyboardStateBuffer.AtOffset(-frame - 1).IsKeyUp(key);
        }

        public bool IsKeyUp(Keys key, int frame = 0)
        {
            if (frame >= StatesCount)
                throw new ArgumentOutOfRangeException(nameof(frame), $"{nameof(frame)} >= {StatesCount}");

            return keyboardStateBuffer.AtOffset(-frame).IsKeyUp(key);
        }

        public bool IsKeyReleased(Keys key, int frame = 0)
        {
            if (frame >= StatesCount)
                throw new ArgumentOutOfRangeException(nameof(frame), $"{nameof(frame)} >= {StatesCount}");

            return keyboardStateBuffer.AtOffset(-frame - 1).IsKeyDown(key) &&
                   keyboardStateBuffer.AtOffset(-frame).IsKeyUp(key);
        }

        public bool IsCapsLockToggled(int frame = 0)
        {
            if (frame >= StatesCount)
                throw new ArgumentOutOfRangeException(nameof(frame), $"{nameof(frame)} >= {StatesCount}");

            return keyboardStateBuffer.AtOffset(-frame).CapsLock;
        }

        public TimeSpan GetKeyTimeDown(Keys key)
            => keyWatcher.TimeActive(key);

        public TimeSpan GetKeyTimeUp(Keys key)
            => keyWatcher.TimeInactive(key);

        public void Poll(IGameEngineTime time)
        {
            keyboardStateBuffer.Push(Keyboard.GetState());

            if (keyboardCharacterFrameBuffer.Length != 0)
            {
                keyboardCharacterBuffer.Push(keyboardCharacterFrameBuffer.ToString());

                keyboardCharacterFrameBuffer.Clear();
            }
            else
                keyboardCharacterBuffer.Push(string.Empty);

            keyWatcher.Update(time, GetKeysUp(), GetKeysDown());
        }
    }
}