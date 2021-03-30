using System;
using Fracture.Common;
using Fracture.Engine.Core;
using Fracture.Engine.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shattered.Content.Ui;

namespace Fracture.Engine.Ui.Controls
{
    public enum ImageMode : byte 
    {
        Normal,
        Source,
        Fit
    }
    
    public sealed class ImageBox : Control
    {
        #region Properties
        public ImageMode ImageMode
        {
            get;
            set;
        }

        public Texture2D Image
        {
            get;
            set;
        }

        public Rectangle ImageSource
        {
            get;
            set;
        }

        public SpriteEffects ImageEffects
        {
            get;
            set;
        }

        public Color ImageColor
        {
            get;
            set;
        }
        #endregion

        public ImageBox()
        {
            Size       = new Vector2(0.25f);
            ImageColor = Color.White;
        }
        
        protected override void InternalDraw(IGraphicsFragment fragment, IGameEngineTime time)
        {
            if (Image == null) return;

            var destination = GetRenderDestinationRectangle();
            var imageOffset = Style.Get<Vector2>($"{UiStyleKeys.Target.ImageBox}\\{UiStyleKeys.Offset.Normal}");

            var imageWidth  = (int)Math.Floor(destination.Width * imageOffset.X);
            var imageHeight = (int)Math.Floor(destination.Height * imageOffset.Y);

            var imageX = destination.X + imageWidth * (1.0f - imageOffset.X) * 0.5f;
            var imageY = destination.Y + imageHeight * (1.0f - imageOffset.Y) * 0.5f;

            switch (ImageMode)
            {
                case ImageMode.Fit:
                case ImageMode.Normal:
                    fragment.DrawSprite(new Vector2(imageX, imageY),
                                        Vector2.One,
                                        0.0f,
                                        Vector2.Zero,
                                        new Vector2(destination.Width, destination.Height),
                                        Image,
                                        ImageColor);
                    break;
                case ImageMode.Source:
                    
                    fragment.DrawSprite(new Vector2(imageX, imageY),
                                        Vector2.One,
                                        0.0f,
                                        Vector2.Zero,
                                        new Vector2(destination.Width, destination.Height),
                                        ImageSource == Rectangle.Empty ? Image.Bounds : ImageSource,
                                        Image,
                                        ImageColor);
                    break;
                default:
                    throw new InvalidOrUnsupportedException(nameof(ImageMode), ImageMode);
            }
        }
    }
}
