using System;
using System.Text;
using Fracture.Engine.Core;
using Fracture.Engine.Graphics;
using Fracture.Engine.Input.Devices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Fracture.Engine.Ui.Controls
{
    /// <summary>
    /// Text input field that can be used for plain text input and hidden input for sensitive
    /// strings such as passwords.
    /// </summary>
    public sealed class TextInput : Control
    {
        #region Fields
        private string displayText;
        private string text;

        private bool caretVisible;

        private TimeSpan caretMove;
        private TimeSpan caretBlink;
        private TimeSpan caretErase;
        #endregion

        #region Events
        public event EventHandler TextChanged;
        #endregion

        #region Properties
        /// <summary>
        /// Text that will be displayed if the field has no string entered and
        /// the control has no focus.
        /// </summary>
        public string PlaceholderText
        {
            get;
            set;
        }

        /// <summary>
        /// Actual text in the field.
        /// </summary>
        public string Text
        {
            get => text;
            set
            {
                var old = text;
                text    = value;

                if (text != old)
                    TextChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Character used to hide sensitive information.
        /// </summary>
        public char Mask
        {
            get;
            set;
        }

        /// <summary>
        /// Color of the text inside the field.
        /// </summary>
        public Color TextColor
        {
            get;
            set;
        }

        /// <summary>
        /// Position of the caret inside the field.
        /// </summary>
        public int CaretPosition
        {
            get;
            private set;
        }
        #endregion

        public TextInput()
        {
            Text        = string.Empty;
            displayText = string.Empty;

            Size      = new Vector2(0.35f, 0.10f);
            TextColor = Color.White;
        }

        protected override void InternalReceiveTextInput(IGameEngineTime time, string text)
        {
            if (!HasFocus) return;

            var oldLength = Text.Length;

            Text = Text.Insert(MathHelper.Clamp(CaretPosition, 0, Text.Length), text);
            
            if (CaretPosition == oldLength) CaretPosition = Text.Length;
            else                            CaretPosition++;
        }

        protected override void InternalReceiveKeyboardInput(IGameEngineTime time, IKeyboardDevice keyboard)
        {
            if (!HasFocus) return;

            // Move caret left.
            UpdateCaretLeft(time);

            // Move caret right.
            UpdateCaretRight(time);

            // Erase.
            UpdateCaretErase(time);
        }

        private void UpdateCaretLeft(IGameEngineTime time)
        {
            if (CaretPosition == 0)
                return;
            
            if (Keyboard.IsPressed(Keys.Left) || (Keyboard.IsDown(Keys.Left) &&
                                                  Keyboard.TimeDown(Keys.Left) >= TimeSpan.FromMilliseconds(250) &&
                                                  caretMove >= TimeSpan.FromMilliseconds(100)))
            {
                CaretPosition--;

                caretMove = TimeSpan.Zero;
            }

            caretMove += time.Elapsed;
        }

        private void UpdateCaretRight(IGameEngineTime time)
        {
            if (CaretPosition == Text.Length) return;
            
            if (Keyboard.IsPressed(Keys.Right) || (Keyboard.IsDown(Keys.Right) &&
                                                   Keyboard.TimeDown(Keys.Right) >= TimeSpan.FromMilliseconds(250) &&
                                                   caretMove >= TimeSpan.FromMilliseconds(100)))
            {
                CaretPosition++;

                caretMove = TimeSpan.Zero;
            }

            caretMove += time.Elapsed;
        }

        private void UpdateCaretErase(IGameEngineTime time)
        {
            if (Text.Length == 0 || CaretPosition == 0) return;
            
            if (Keyboard.IsPressed(Keys.Back) || (Keyboard.IsDown(Keys.Back) &&
                                                  Keyboard.TimeDown(Keys.Back) >= TimeSpan.FromMilliseconds(250) &&
                                                  caretErase >= TimeSpan.FromMilliseconds(100)))
            {
                Text = Text.Remove(CaretPosition - 1, 1);

                CaretPosition = MathHelper.Clamp(CaretPosition - 1, 0, CaretPosition);

                caretErase = TimeSpan.Zero;
            }

            caretErase += time.Elapsed;
        }

        private void UpdateCaretBlink(IGameEngineTime time)
        {
            if (!HasFocus && string.IsNullOrEmpty(Text))
                displayText = PlaceholderText;
            else
            {
                // Hide password or display the actual text.
                displayText = !char.IsWhiteSpace(Mask) ? new string(Mask, Text.Length) : Text;

                // Show caret.
                caretVisible  = HasFocus && caretBlink != TimeSpan.Zero && caretBlink <= TimeSpan.FromMilliseconds(250);
                CaretPosition = MathHelper.Clamp(CaretPosition, 0, displayText.Length); 

                if (caretVisible)
                    displayText = displayText.Insert(CaretPosition, "|");
                else if (caretBlink >= TimeSpan.FromMilliseconds(500))
                    caretBlink = TimeSpan.Zero;

                caretBlink += time.Elapsed;
            }
        }
        
        private void UpdateDisplayText()
        {
            if (string.IsNullOrEmpty(displayText)) return;

            // Clip text to fit area.
            var target = Style.Get<Texture2D>($"{UiStyleKeys.Target.TextInput}\\{UiStyleKeys.Texture.Normal}").Bounds;
            var font   = Style.Get<SpriteFont>($"{UiStyleKeys.Target.TextInput}\\{UiStyleKeys.Font.Normal}");
            var source = Style.Get<Rectangle>($"{UiStyleKeys.Target.TextInput}\\{UiStyleKeys.Source.Text}");

            var destination = GetRenderDestinationRectangle();
            
            var scale = GraphicsUtils.ScaleToTarget(new Vector2(destination.Width, destination.Height), 
                                                           new Vector2(target.Width, target.Height));

            var area  = UiUtils.ScaleTextArea(new Vector2(destination.X, destination.Y), scale, source);
            var local = new Rectangle(0, 0, area.Width, area.Height);

            if (!(font.MeasureString(displayText).X >= area.Width)) return;
            
            // TODO: fix to work like HTML text input fields.
            var head       = string.Empty;
            var headLength = 0;
            var i          = CaretPosition - (caretVisible ? 0 : 1);
                
            while (i >= 0)
            {
                var sub  = displayText.Substring(i, 1);
                var size = (int)Math.Floor(font.MeasureString(sub).X);

                if (headLength + size >= local.Width)
                    break;

                headLength += size;
                head       = sub + head;

                i--;
            }

            var sb         = new StringBuilder();
            var tailLength = headLength;

            i = MathHelper.Clamp(CaretPosition - (caretVisible ? 0 : 1) + 1, 0, displayText.Length);

            while (tailLength < local.Width && i < displayText.Length)
            {
                var sub = displayText.Substring(i, 1);

                tailLength += (int)Math.Floor(font.MeasureString(sub).X);

                sb.Append(sub);

                i++;
            }
                
            displayText = head + sb;
        }

        protected override void InternalUpdate(IGameEngineTime time)
        {
            UpdateCaretBlink(time);

            UpdateDisplayText();
        }
        
        protected override void InternalDraw(IGraphicsFragment fragment, IGameEngineTime time)
        {
            var texture = Style.Get<Texture2D>($"{UiStyleKeys.Target.TextInput}\\{UiStyleKeys.Texture.Normal}");
            var color   = Style.Get<Color>($"{UiStyleKeys.Target.TextInput}\\{(Enabled ? UiStyleKeys.Color.Enabled : UiStyleKeys.Color.Disabled)}");
            var font    = Style.Get<SpriteFont>($"{UiStyleKeys.Target.TextInput}\\{UiStyleKeys.Font.Normal}");
            var source  = Style.Get<Rectangle>($"{UiStyleKeys.Target.TextInput}\\{UiStyleKeys.Source.Text}");
            
            var destination = GetRenderDestinationRectangle();
            
            fragment.DrawSprite(new Vector2(destination.X, destination.Y), 
                                Vector2.One, 
                                0.0f, 
                                Vector2.Zero, 
                                new Vector2(destination.Width, destination.Height),
                                texture, 
                                color);

            if (string.IsNullOrEmpty(displayText)) return;
            
            var scale = GraphicsUtils.ScaleToTarget(new Vector2(destination.Width, destination.Height), 
                                                           new Vector2(texture.Width, texture.Height));
            
            var textArea     = UiUtils.ScaleTextArea(new Vector2(destination.X, destination.Y), scale, source);
            var textCenter   = destination.Height / 2.0f - font.MeasureString(displayText).Y * 0.5f;
            var textPosition = new Vector2(textArea.X, destination.Y);

            fragment.DrawSpriteText(textPosition + textCenter * Vector2.UnitY,
                                    Vector2.One,
                                    0.0f,
                                    Vector2.Zero,
                                    displayText,
                                    font,
                                    TextColor);
        }
    }
}
