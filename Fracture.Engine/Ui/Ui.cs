using System;
using Fracture.Engine.Core;
using Fracture.Engine.Graphics;
using Fracture.Engine.Input.Devices;
using Fracture.Engine.Ui.Components;
using Fracture.Engine.Ui.Controls;

namespace Fracture.Engine.Ui
{
    /// <summary>
    /// Class that contains single user interface or screen.
    /// </summary>
    public sealed class Ui : IDisposable
    {
        #region Fields
        private readonly ControlKeyboardFocusManager keyboardFocusManager;
        private readonly ControlMouseFocusManager    mouseFocusManager;

        private readonly IKeyboardDevice keyboard;
        private readonly IMouseDevice    mouse;
        #endregion

        #region Properties
        public IStaticContainerControl Root
        {
            get;
        }

        public IView View
        {
            get;
        }

        public string Name
        {
            get;
        }
        #endregion

        public Ui(string name,
                  IView view,
                  IStaticContainerControl root,
                  IMouseDevice mouse,
                  IKeyboardDevice keyboard)
        {
            Name = !string.IsNullOrEmpty(name) ? name : throw new ArgumentNullException(nameof(name));
            View = view ?? throw new ArgumentNullException(nameof(view));
            Root = root ?? throw new ArgumentNullException(nameof(root));

            this.mouse    = mouse ?? throw new ArgumentNullException(nameof(mouse));
            this.keyboard = keyboard ?? throw new ArgumentNullException(nameof(keyboard));

            var context = new ControlFocusManagerContext();

            mouseFocusManager    = new ControlMouseFocusManager(root, context);
            keyboardFocusManager = new ControlKeyboardFocusManager(root, context);

            UiCanvas.ScreenSizeChanged += UserInterfaceCanvas_ScreenSizeChanged;
        }

        #region Event handlers
        private void UserInterfaceCanvas_ScreenSizeChanged(object sender, EventArgs e)
            => Root.UpdateChildrenLayout();
        #endregion

        public void Update(IGameEngineTime time)
        {
            // Update focus.
            mouseFocusManager.Update(time, mouse);
            keyboardFocusManager.Update(time, keyboard);

            // Update root.
            var text = keyboard.GetCharacters();

            Root.ReceiveMouseInput(time, mouse);
            Root.ReceiveKeyboardInput(time, keyboard);

            if (!string.IsNullOrEmpty(text))
                Root.ReceiveTextInput(time, text);

            // Do control specific updates.
            Root.Update(time);
        }

        public void BeforeDraw(IGraphicsFragment fragment, IGameEngineTime time)
            => Root.BeforeDraw(fragment, time);

        public void Draw(IGraphicsFragment fragment, IGameEngineTime time)
            => Root.Draw(fragment, time);

        public void AfterDraw(IGraphicsFragment fragment, IGameEngineTime time)
            => Root.AfterDraw(fragment, time);

        public void Dispose()
            => Root.Dispose();
    }
}