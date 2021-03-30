using System;
using Fracture.Engine.Core;
using Fracture.Engine.Graphics;
using Fracture.Engine.Input.Devices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shattered.Content.Ui;

namespace Fracture.Engine.Ui.Controls
{
    public enum ButtonState
    {
        Released,
        Down
    }
    
    public class Button : Control
    {
        #region Events
        public event EventHandler Click;
        public event EventHandler Down;
        public event EventHandler Released;
        #endregion

        #region Fields
        private ButtonState state;
        #endregion
        
        #region Properties
        public string Text
        {
            get;
            set;
        }
        #endregion

        public Button()
        {
            Size = new Vector2(0.25f, 0.15f);
            
            FocusChanged += Button_FocusChanged;
        }

        #region Event handlers
        private void Button_FocusChanged(object sender, EventArgs e)
        {
            if (!HasFocus)
                state = ButtonState.Released;
        }
        #endregion

        protected virtual void InternalClick()
            => Click?.Invoke(this, EventArgs.Empty);

        protected virtual void InternalDown()
            => Down?.Invoke(this, EventArgs.Empty);

        protected virtual void InternalReleased()
            => Released?.Invoke(this, EventArgs.Empty);
        
        protected sealed override void InternalReceiveMouseInput(IGameEngineTime time, IMouseDevice mouse)
        {
            if (!HasFocus) return;
            
            if (Mouse.IsDown(MouseButton.Left))
            {
                if (state == ButtonState.Released)
                {
                    InternalClick();
                    
                    state = ButtonState.Down;
                }
                else
                    InternalDown();
            }
            else
            {
                if (state == ButtonState.Released)
                    return;
                
                InternalReleased();
                
                state = ButtonState.Released;
            }
        }

        protected virtual Texture2D GetStyleStateTexture()
            => Style.Get<Texture2D>($"{UiStyleKeys.Target.Button}\\{(Mouse.IsHovering(this) ? UiStyleKeys.Texture.Hover : UiStyleKeys.Texture.Normal)}");

        protected virtual Point GetStyleStateOffset()
        {
            if ((!HasFocus || !Mouse.IsHovering(this)) ||
                (!Mouse.IsDown(MouseButton.Left) && !Mouse.IsPressed(MouseButton.Left))) 
                return Point.Zero;
            
            var offset = Style.Get<Vector2>($"{UiStyleKeys.Target.Button}\\{UiStyleKeys.Offset.Click}");

            return new Point((int)Math.Floor(offset.X), (int)Math.Floor(offset.Y));
        }
            
        protected override void InternalDraw(IGraphicsFragment fragment, IGameEngineTime time)
        {
            var texture     = GetStyleStateTexture();
            var color       = Style.Get<Color>($"{UiStyleKeys.Target.Button}\\{(Enabled ? UiStyleKeys.Color.Enabled : UiStyleKeys.Color.Disabled)}");
            var center      = Style.Get<Rectangle>($"{UiStyleKeys.Target.Button}\\{UiStyleKeys.Source.Center}");
            var font        = Style.Get<SpriteFont>($"{UiStyleKeys.Target.Button}\\{UiStyleKeys.Font.Normal}");
            var destination = GetRenderDestinationRectangle();
            var offset      = GetStyleStateOffset();

            destination.X += offset.X;
            destination.Y += offset.Y;

            fragment.DrawSurface(texture, center, destination, color);

            if (string.IsNullOrEmpty(Text))
                return;
            
            var position = new Vector2(destination.X, destination.Y) + UiCanvas.ToScreenUnits(ActualSize) * 0.5f - font.MeasureString(Text) * 0.5f;

            fragment.DrawSpriteText(position, 
                                    Vector2.One, 
                                    0.0f, 
                                    Vector2.Zero, 
                                    font.MeasureString(Text),
                                    Text, 
                                    font,
                                    color);
        }
    }
}
