using System;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fracture.Engine.Ui
{
    public static class UiUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 SquareToTarget(float diameter, in Vector2 targetActualSize)
        {
            var sx = 0.0f;
            var sy = 0.0f;

            if (targetActualSize.X > targetActualSize.Y)
            {
                // Y to X ration or aspect ratio.
                var yx = (targetActualSize.Y) / (targetActualSize.X);

                // Difference between.
                var dxy = (targetActualSize.X - targetActualSize.Y) / (targetActualSize.X);

                // Scale to aspect ration.
                var adx = yx * diameter;

                // Scale to difference.
                var pdx = diameter * dxy;

                // In case aspect ratio can be kept, use it directly.
                // In other case, reduce difference scaled value
                // from the aspect ratio scaled value to keep the
                // square.
                if (adx < pdx)
                    sx = adx;
                else
                    sx = adx - diameter * dxy;

                sy = diameter;
            }
            else
            {
                sx = diameter;

                var xy  = (targetActualSize.X) / (targetActualSize.Y);
                var dyx = (targetActualSize.Y - targetActualSize.X) / (targetActualSize.Y);
                var ady = xy * diameter;
                var pdy = diameter * ady;

                if (ady < pdy)
                    sy = ady;
                else
                    sy = ady - diameter * dyx;
            }

            return new Vector2(sx, sy);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Rectangle ScaleTextArea(in Vector2 position, in Vector2 scale, in Rectangle source) =>
            new Rectangle((int)Math.Floor(source.X + position.X),
                          (int)Math.Floor(source.Y + position.Y),
                          (int)Math.Floor(source.Width * scale.X),
                          (int)Math.Floor(source.Height * scale.Y));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ClipText(string text, Rectangle area, SpriteFont font)
        {
            var i = 0;

            var sb = new StringBuilder();

            while (true)
            {
                var token = text.Substring(i++, 1);

                if (font.MeasureString(sb).X + font.MeasureString(token).X >= area.Width)
                    break;

                sb.Append(token);
            }

            return sb.ToString();
        }
    }
}