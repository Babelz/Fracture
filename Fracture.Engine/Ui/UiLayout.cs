using System;
using System.Runtime.CompilerServices;
using Fracture.Common.Util;
using Microsoft.Xna.Framework;

namespace Fracture.Engine.Ui
{
    public enum Orientation : byte
    {
        Horizontal = 0,
        Vertical,
    }

    /// <summary>
    /// Defines anchoring modes. Anchors define where
    /// element is to be positioned when anchoring position
    /// mode is used.
    /// </summary>
    [Flags]
    public enum Anchor : ushort
    {
        None = 0,

        /// <summary>
        /// Anchors element to the top of the parent.
        /// </summary>
        Top = 1 << 0,

        /// <summary>
        /// Anchors the element to the left of the parent.
        /// </summary>
        Left = 1 << 1,

        /// <summary>
        /// Anchors the element to the bottom of the parent.
        /// </summary>
        Bottom = 1 << 2,

        /// <summary>
        /// Anchors the element to the right of the parent.
        /// </summary>
        Right = 1 << 3,

        /// <summary>
        /// Anchors the element to the center of the parent.
        /// </summary>
        Center = 1 << 4,
    }

    /// <summary>
    /// Defines positioning modes. Positioning modes
    /// define how the element is positioned.
    /// </summary>
    public enum Positioning : byte
    {
        /// <summary>
        /// Positioned using relative to parent positioning, position is relative. Allows
        /// control to be dragged.
        /// </summary>
        Relative,

        /// <summary>
        /// Positioned using anchor, position is relative position.
        /// </summary>
        Anchor,

        /// <summary>
        /// Positioned using anchor and offset value, position is offset. Allows
        /// control to be dragged.
        /// </summary>
        Offset,

        /// <summary>
        /// Positioned using absolute positioning, position is absolute. Allows 
        /// to be dragged.
        /// </summary>
        Absolute,
    }

    public readonly struct UiOffset
    {
        #region Static fields
        public static readonly UiOffset Zero = new UiOffset(0.0f);
        #endregion

        #region Fields
        public readonly float Top;
        public readonly float Left;
        public readonly float Bottom;
        public readonly float Right;
        #endregion

        public UiOffset(float top, float left, float bottom, float right)
        {
            Top    = top;
            Left   = left;
            Bottom = bottom;
            Right  = right;
        }

        public UiOffset(float value)
            : this(value, value, value, value)
        {
        }

        public override int GetHashCode()
            => HashUtils.Create()
                        .Append(Top)
                        .Append(Left)
                        .Append(Bottom)
                        .Append(Right);

        public override bool Equals(object obj)
            => obj is UiOffset other && this == other;

        public static bool operator ==(in UiOffset lhs, in UiOffset rhs)
            => Math.Abs(lhs.Left - rhs.Left) < 0.1f &&
               Math.Abs(lhs.Right - rhs.Right) < 0.1f &&
               Math.Abs(lhs.Top - rhs.Top) < 0.1f &&
               Math.Abs(lhs.Bottom - rhs.Bottom) < 0.1f;

        public static bool operator !=(in UiOffset lhs, in UiOffset rhs)
            => !(lhs == rhs);

        public static UiOffset operator +(in UiOffset lhs, in UiOffset rhs)
            => new UiOffset(lhs.Top + rhs.Top,
                            lhs.Left + rhs.Left,
                            lhs.Bottom + rhs.Bottom,
                            lhs.Right + rhs.Right);

        public static UiOffset operator -(in UiOffset lhs, in UiOffset rhs)
            => new UiOffset(lhs.Top - rhs.Top,
                            lhs.Left - rhs.Left,
                            lhs.Bottom - rhs.Bottom,
                            lhs.Right - rhs.Right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UiOffset ToTop(float value)
            => new UiOffset(value, 0.0f, 0.0f, 0.0f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UiOffset ToLeft(float value)
            => new UiOffset(0.0f, value, 0.0f, 0.0f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UiOffset ToBottom(float value)
            => new UiOffset(0.0f, 0.0f, value, 0.0f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UiOffset ToRight(float value)
            => new UiOffset(0.0f, 0.0f, 0.0f, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UiOffset Transform(in UiOffset uiOffset, in Vector2 scale)
            => new UiOffset(uiOffset.Top * scale.Y,
                            uiOffset.Left * scale.X,
                            uiOffset.Bottom * scale.Y,
                            uiOffset.Right * scale.X);
    }
}