using System;
using Microsoft.Xna.Framework;

namespace Fracture.Engine.Graphics
{
    public interface IWindow
    {
        #region Properties
        public IntPtr Handle
        {
            get;
        }
        #endregion
        
        #region Events
        event EventHandler<TextInputEventArgs> TextInput;
        #endregion
    }
    
    public sealed class Window : IWindow
    {
        #region Fields
        private readonly GameWindow window;
        #endregion

        #region Events
        public event EventHandler<TextInputEventArgs> TextInput;
        #endregion
        
        #region Properties
        public IntPtr Handle
            => window.Handle;
        #endregion

        public Window(GameWindow window)
        {
            this.window = window ?? throw new ArgumentNullException(nameof(window));
            
            window.TextInput += WindowOnTextInput;
        }

        #region Event handlers
        private void WindowOnTextInput(object sender, TextInputEventArgs e)
            => TextInput?.Invoke(this, e);
        #endregion
    }
}