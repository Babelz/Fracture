using System;
using Fracture.Client.Content.Ui;
using Fracture.Engine.Core;
using Fracture.Engine.Graphics;
using Fracture.Engine.Ui.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fracture.Engine.Ui.Controls
{
    /// <summary>
    /// Basic container that supports multiple child elements and header text.
    /// </summary>
    public class HeaderPanel : DynamicContainerControl
    {
        #region Fields
        private Paragraph header;
        #endregion

        #region Properties
        public string HeaderText
        {
            get => header.Text;
            set
            {
                header.Text = value;

                CenterHeaderParagraph();
            }
        }

        public string HeaderFont
        {
            get => header.Font;
            set
            {
                header.Font = value;

                CenterHeaderParagraph();
            }
        }
        #endregion

        public HeaderPanel()
        {
            CreateHeaderParagraph();
        }

        public HeaderPanel(IControlManager controls)
            : base(controls)
        {
            CreateHeaderParagraph();
        }

        #region Event handlers
        private void Header_StyleChanged(object sender, EventArgs e)
            => CenterHeaderParagraph();
        #endregion

        private void CenterHeaderParagraph()
        {
            if (header.Style == null) return;

            var centerArea = Style.Get<Rectangle>($"{UiStyleKeys.Target.HeaderPanel}\\{UiStyleKeys.Source.Center}");
            var headerArea = Style.Get<Rectangle>($"{UiStyleKeys.Target.HeaderPanel}\\{UiStyleKeys.Source.Header}");

            var centerToHeaderRatio = headerArea.Height / ((float)centerArea.Height + headerArea.Height);
            var totalHeight         = ActualSize.Y * centerToHeaderRatio;
            var headerActualHeight  = header.ActualSize.Y;

            var headerX = 0.5f - header.Size.X * 0.5f;
            var headerY = totalHeight > headerActualHeight ? 
                          totalHeight - header.ActualSize.Y * 0.5f :
                          totalHeight;
            
            header.Position = new Vector2(headerX, headerY);
        }

        private void CreateHeaderParagraph()
        {
            header = new Paragraph
            {
                Text             = "header-panel",
                Font             = UiStyleKeys.Font.Normal,
                KeepInTextBounds = true,
                Positioning      = Positioning.Relative
            };

            header.StyleChanged += Header_StyleChanged;

            Add(header);
        }
        
        protected override void InternalDraw(IGraphicsFragment fragment, IGameEngineTime time)
        {
            var texture     = Style.Get<Texture2D>($"{UiStyleKeys.Target.HeaderPanel}\\{UiStyleKeys.Texture.Normal}");
            var color       = Style.Get<Color>($"{UiStyleKeys.Target.HeaderPanel}\\{(Enabled ? UiStyleKeys.Color.Enabled : UiStyleKeys.Color.Disabled)}");
            var center      = Style.Get<Rectangle>($"{UiStyleKeys.Target.HeaderPanel}\\{UiStyleKeys.Source.Center}");
            var destination = GetRenderDestinationRectangle();

            fragment.DrawSurface(texture, center, destination, color);
            
            base.InternalDraw(fragment, time);
        }
    }
}
