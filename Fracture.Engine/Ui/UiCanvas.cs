using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;

namespace Fracture.Engine.Ui
{
    /// <summary>
    /// Static utility class that handles unit conversions from
    /// local space to screen space and the other way around. Controls
    /// transform range from 0.0 to 1.0.
    /// </summary>
    public static class UiCanvas
    {
        #region Fields
        private static Vector2 factor;
        #endregion

        #region Properties
        public static int Width
        {
            get;
            private set;
        }
        public static int Height
        {
            get;
            private set;
        }
        #endregion

        #region Events
        public static event EventHandler ScreenSizeChanged;
        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetSize(int width, int height)
        {
            var oldWidth  = Width;
            var oldHeight = Height;

            Width  = width < 0 ? throw new ArgumentOutOfRangeException(nameof(width)) : width;
            Height = height < 0 ? throw new ArgumentOutOfRangeException(nameof(height)) : height;

            if (width != oldWidth || height != oldHeight)
            {
                factor = new Vector2(1.0f / width, 1.0f / height);

                ScreenSizeChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Converts from screen units to local units.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ToLocalUnits(Vector2 screenUnits)
            => screenUnits * factor;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ToLocalUnits(float x, float y)
            => new Vector2(x * factor.X, y * factor.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToVerticalLocalUnits(float value)
            => value * factor.Y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToHorizontalLocalUnits(float value)
            => value * factor.X;

        /// <summary>
        /// Converts from local units to screen units.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ToScreenUnits(Vector2 localUnits)
            => new Vector2(localUnits.X * Width, localUnits.Y * Height);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ToScreenUnits(float x, float y)
            => new Vector2(x * Width, y * Height);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToVerticalScreenUnits(float value)
            => value * Height;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToHorizontalScreenUnits(float value)
            => value * Width;
    }
}
