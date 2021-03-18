using Fracture.Engine.Core;
using Fracture.Engine.Graphics;
using Fracture.Engine.Ui.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fracture.Engine.Ui.Controls
{
    /// <summary>
    /// Basic container that supports multiple child elements.
    /// </summary>
    public class Panel : DynamicContainerControl
    {
        public Panel()
        {
        }

        public Panel(IControlManager controls) 
            : base(controls)
        {
        }

        protected override void InternalDraw(IGraphicsFragment fragment, IGameEngineTime time)
        {
            var texture     = Style.Get<Texture2D>($"{UiStyleKeys.Target.Panel}\\{UiStyleKeys.Texture.Normal}");
            var color       = Style.Get<Color>($"{UiStyleKeys.Target.Panel}\\{(Enabled ? UiStyleKeys.Color.Enabled : UiStyleKeys.Color.Disabled)}");
            var center      = Style.Get<Rectangle>($"{UiStyleKeys.Target.Panel}\\{UiStyleKeys.Source.Center}");
            var destination = GetRenderDestinationRectangle();

            fragment.DrawSurface(texture, center, destination, color);
            
            base.InternalDraw(fragment, time);
        }
    }
}
