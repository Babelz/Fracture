using System;
using Fracture.Engine.Core;
using Fracture.Engine.Graphics;
using Fracture.Engine.Input.Devices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shattered.Content.Ui;

namespace Fracture.Engine.Ui.Controls
{
    public sealed class SliderValueChangedEventArgs : EventArgs
    {
        #region Properties
        public uint OldValue
        {
            get;
        }

        public uint NextValue
        {
            get;
        }

        public uint MaxValue
        {
            get;
        }
        #endregion

        public SliderValueChangedEventArgs(uint oldValue, uint nextValue, uint maxValue)
        {
            OldValue  = oldValue;
            NextValue = nextValue;
            MaxValue  = maxValue;
        }
    }

    public sealed class SliderMaxValueChangedEventArgs : EventArgs
    {
        #region Properties
        public uint OldMax
        {
            get;
        }

        public uint NextMax
        {
            get;
        }
        #endregion

        public SliderMaxValueChangedEventArgs(uint oldMax, uint nextMax)
        {
            OldMax  = oldMax;
            NextMax = nextMax;
        }
    }

    public sealed class Slider : Control
    {
        #region Fields
        private uint maxValue;
        private uint currentValue;

        private TimeSpan downElapsed;
        #endregion

        #region Events
        public event EventHandler<SliderValueChangedEventArgs> CurrentValueChanged;

        public event EventHandler<SliderMaxValueChangedEventArgs> MaxValueChanged;
        #endregion

        #region Properties
        public Orientation Orientation
        {
            get;
            set;
        }

        public uint MaxValue
        {
            get => maxValue;
            set
            {
                var oldMaxValue = maxValue;

                maxValue = value;

                if (maxValue != oldMaxValue)
                {
                    CurrentValue = CurrentValue > maxValue ? maxValue : CurrentValue;

                    MaxValueChanged?.Invoke(this, new SliderMaxValueChangedEventArgs(oldMaxValue, maxValue));
                }
            }
        }

        public uint CurrentValue
        {
            get => currentValue;
            set
            {
                var oldCurrentValue = currentValue;

                currentValue = (uint)MathHelper.Clamp(value, 0, maxValue);

                if (currentValue != oldCurrentValue)
                    CurrentValueChanged?.Invoke(this, new SliderValueChangedEventArgs(oldCurrentValue, currentValue, maxValue));
            }
        }
        #endregion

        public Slider() => Size = new Vector2(0.25f, 0.1f);

        private (Rectangle Left, Rectangle Center, Rectangle Right, Rectangle Slider) GetActualRenderDestinationRectangles()
        {
            const float CenterHeight = 0.5f;

            var destination = GetRenderDestinationRectangle();

            Rectangle left;
            Rectangle center;
            Rectangle right;
            Rectangle slider;

            if (Orientation == Orientation.Horizontal)
            {
                left = new Rectangle(destination.X,
                                     destination.Y,
                                     destination.Height,
                                     destination.Height);

                center = new Rectangle(destination.X,
                                       destination.Y + (int)Math.Floor(destination.Height * CenterHeight * 0.5f),
                                       destination.Width,
                                       (int)Math.Floor(destination.Height * CenterHeight));

                right = new Rectangle(center.Right - destination.Height,
                                      destination.Y,
                                      destination.Height,
                                      destination.Height);

                int x;

                if (maxValue == 0 || currentValue == 0) x = left.Right;
                else if (currentValue == maxValue) x      = right.Left - destination.Height;
                else x                                    = left.Right + (int)((right.Left - left.Right - destination.Height) / maxValue * currentValue);

                slider = new Rectangle(x,
                                       destination.Y,
                                       destination.Height,
                                       destination.Height);
            }
            else
            {
                left = new Rectangle(destination.X,
                                     destination.Y,
                                     destination.Width,
                                     destination.Width);

                center = new Rectangle(destination.X + (int)Math.Floor(destination.Width * CenterHeight * 0.5f),
                                       destination.Y + (int)Math.Floor(destination.Width * 0.5f),
                                       (int)Math.Floor(destination.Width * CenterHeight),
                                       destination.Height - (int)Math.Floor(destination.Width * 0.5f));

                right = new Rectangle(destination.X,
                                      destination.Bottom - destination.Width,
                                      destination.Width,
                                      destination.Width);

                int y;

                if (maxValue == 0 || currentValue == 0) y = left.Bottom;
                else if (currentValue == maxValue) y      = right.Top - destination.Width;
                else y                                    = left.Bottom + (int)((right.Top - left.Bottom - destination.Width) / maxValue * currentValue);

                slider = new Rectangle(destination.X,
                                       y,
                                       destination.Width,
                                       destination.Width);
            }

            return (left, center, right, slider);
        }

        private bool AcceptScrollInput()
        {
            if (Mouse.ScrollValueChanged)
            {
                // If focused and mouse scrolled, handle scrolling input.
                var delta = Mouse.LastScrollValue - Mouse.CurrentScrollValue;

                if (delta < 0) Rewind(1);
                else Forward(1);

                return true;
            }

            return false;
        }

        private bool AcceptButtonInput(IGameEngineTime time)
        {
            if (!Mouse.IsPressed(MouseButton.Left) && !Mouse.IsDown(MouseButton.Left)) return false;

            // If we intersect buttons, handle clicks based on them.
            var (left, _, right, _) = GetActualRenderDestinationRectangles();

            var forward = Mouse.IsHovering(right);
            var rewind  = Mouse.IsHovering(left);

            if (forward || rewind)
            {
                if (Mouse.TimeDown(MouseButton.Left) == TimeSpan.Zero)
                {
                    if (forward) Forward(1);
                    else Rewind(1);
                }
                else if (Mouse.TimeDown(MouseButton.Left) >= TimeSpan.FromMilliseconds(125) && downElapsed >= TimeSpan.FromMilliseconds(125))
                {
                    if (forward) Forward(1);
                    else Rewind(1);

                    downElapsed = TimeSpan.Zero;
                }

                downElapsed += time.Elapsed;

                return true;
            }

            downElapsed = TimeSpan.Zero;

            return false;
        }

        private void AcceptClickInput()
        {
            var (left, center, right, _) = GetActualRenderDestinationRectangles();

            if (!Mouse.IsPressed(MouseButton.Left) || !Mouse.IsHovering(center)) return;

            if (Orientation == Orientation.Horizontal)
                CurrentValue = (uint)MathHelper.Clamp(
                    ((int)Math.Floor(Mouse.CurrentScreenPosition.X) - left.X - left.Width * 0.5f) /
                    (right.Left - left.Right) *
                    MaxValue,
                    0,
                    MaxValue);
            else
                CurrentValue = (uint)MathHelper.Clamp(
                    ((int)Math.Floor(Mouse.CurrentScreenPosition.Y) - left.Y - left.Height * 0.75f) /
                    (right.Top - left.Bottom) *
                    MaxValue,
                    0,
                    MaxValue);
        }

        protected override void InternalReceiveMouseInput(IGameEngineTime time, IMouseDevice mouse)
        {
            if (!HasFocus) return;

            if (AcceptScrollInput()) return;
            if (AcceptButtonInput(time)) return;

            AcceptClickInput();
        }

        protected override void InternalDraw(IGraphicsFragment fragment, IGameEngineTime time)
        {
            var textureBackground = Style.Get<Texture2D>($"{UiStyleKeys.Target.Slider}\\{UiStyleKeys.Texture.Normal}");
            var textureSlider     = Style.Get<Texture2D>($"{UiStyleKeys.Target.Slider}\\{UiStyleKeys.Texture.Circle}");
            var color             = Style.Get<Color>($"{UiStyleKeys.Target.Slider}\\{(Enabled ? UiStyleKeys.Color.Enabled : UiStyleKeys.Color.Disabled)}");

            var textureButtonNormal = Style.Get<Texture2D>($"{UiStyleKeys.Target.Button}\\{UiStyleKeys.Texture.Normal}");
            var textureButtonHover  = Style.Get<Texture2D>($"{UiStyleKeys.Target.Button}\\{UiStyleKeys.Texture.Hover}");
            var buttonOffset        = Style.Get<Vector2>($"{UiStyleKeys.Target.Button}\\{UiStyleKeys.Offset.Click}");

            var (left, center, right, slider) = GetActualRenderDestinationRectangles();

            // Draw background.
            fragment.DrawSprite(new Vector2(center.X, center.Y),
                                Vector2.One,
                                0.0f,
                                Vector2.Zero,
                                new Vector2(center.Width, center.Height),
                                textureBackground,
                                color);

            // Draw buttons.
            var pressed     = Mouse.IsPressed(MouseButton.Left) || Mouse.IsDown(MouseButton.Left);
            var leftHover   = Mouse.IsHovering(left);
            var leftPressed = HasFocus && leftHover && pressed;

            var rightHover   = Mouse.IsHovering(right);
            var rightPressed = HasFocus && rightHover && pressed;

            fragment.DrawSprite(new Vector2(left.X, left.Y) + (leftPressed ? buttonOffset : Vector2.Zero),
                                Vector2.One,
                                0.0f,
                                Vector2.Zero,
                                new Vector2(left.Width, left.Height),
                                leftHover ? textureButtonHover : textureButtonNormal,
                                color);


            fragment.DrawSprite(new Vector2(right.X, right.Y) + (rightPressed ? buttonOffset : Vector2.Zero),
                                Vector2.One,
                                0.0f,
                                Vector2.Zero,
                                new Vector2(right.Width, left.Height),
                                rightHover ? textureButtonHover : textureButtonNormal,
                                color);

            // Draw slider.
            fragment.DrawSprite(new Vector2(slider.X, slider.Y),
                                Vector2.One,
                                0.0f,
                                Vector2.Zero,
                                new Vector2(slider.Width, slider.Height),
                                textureSlider,
                                color);
        }

        public void Forward(uint amount) => CurrentValue = currentValue == maxValue ? maxValue : currentValue + amount;

        public void Rewind(uint amount) => CurrentValue = currentValue == 0u ? 0u : currentValue - amount;
    }
}