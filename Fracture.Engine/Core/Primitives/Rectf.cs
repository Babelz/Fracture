using System.Runtime.CompilerServices;
using Fracture.Common.Util;
using Microsoft.Xna.Framework;

namespace Fracture.Engine.Core.Primitives
{
    public readonly struct Rectf
    {
        #region Static properties
        public static readonly Rectf Empty = new Rectf();
        #endregion

        #region Properties
        public Vector2 TopLeft => Position;
        public Vector2 TopRight => new Vector2(Position.X + Bounds.X, Position.Y);

        public Vector2 BottomLeft => new Vector2(Position.X, Position.Y + Bounds.Y);
        public Vector2 BottomRight => Position + Bounds;

        public float Left => Position.X;
        public float Right => Position.X + Bounds.X;
        public float Top => Position.Y;
        public float Bottom => Position.Y + Bounds.Y;

        public float X => Position.X;
        public float Y => Position.Y;

        public float Width => Bounds.X;
        public float Height => Bounds.Y;

        public Vector2 Position
        {
            get;
        }

        public Vector2 Bounds
        {
            get;
        }

        public float Area => Bounds.X * Bounds.Y;
        #endregion

        public Rectf(in Vector2 position, in Vector2 bounds)
        {
            Position = position;
            Bounds   = bounds;
        }

        public override int GetHashCode()
            => HashUtils.Create()
                        .Append(Position.X)
                        .Append(Position.Y)
                        .Append(Bounds.X)
                        .Append(Bounds.Y);

        public override bool Equals(object obj)
        {
            if (obj == null) return false;

            try
            {
                var other = (Rectf)obj;

                return this == other;
            }
            catch
            {
                return false;
            }
        }

        public override string ToString()
            => $"x: {Position.X}, y: {Position.Y}, w: {Bounds.X}, h: {Bounds.Y}, t: {Top}, b: {Bottom}, l: {Left}, r: {Right}";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Intersects(in Rectf a, in Rectf b)
            => a.Left < b.Right &&
               a.Right > b.Left &&
               a.Top < b.Bottom &&
               a.Bottom > b.Top;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Intersects(in Rectf rectf, in Vector2 vector)
        {
            var tlbl = rectf.TopLeft - rectf.BottomLeft;
            var brbl = rectf.BottomRight - rectf.BottomLeft;
            var c    = 2.0f * vector - rectf.TopLeft - rectf.BottomRight;

            return (Vector2.Dot(brbl, c - brbl) <= 0.0f && Vector2.Dot(brbl, c + brbl) >= 0.0f) &&
                   (Vector2.Dot(tlbl, c - tlbl) <= 0.0f && Vector2.Dot(tlbl, c + tlbl) >= 0.0f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Rectf Clamp(in Rectf rectf, in Rectf bounds)
        {
            var x = rectf.X < 0 ? 0 : rectf.X;
            x = x > bounds.Right ? bounds.Right - rectf.Width : x;

            // Clamp y.
            var y = rectf.Y < 0 ? 0 : rectf.Y;
            y = y > bounds.Bottom ? bounds.Bottom - rectf.Height : y;

            // Clamp w.
            var w = rectf.Width;
            w = x + w > bounds.Right ? bounds.Right - x : w;

            // Clamp h.
            var h = rectf.Height;
            h = y + h > bounds.Bottom ? bounds.Bottom - y : h;

            return new Rectf(new Vector2(x, y), new Vector2(w, h));
        }

        public static bool operator ==(in Rectf lhs, in Rectf rhs)
            => lhs.Bounds == rhs.Bounds &&
               lhs.Position == rhs.Position;

        public static bool operator !=(in Rectf lhs, in Rectf rhs)
            => !(lhs == rhs);

        public static explicit operator Rectangle(in Rectf rectf)
            => new Rectangle((int)rectf.X,
                             (int)rectf.Y,
                             (int)rectf.Width,
                             (int)rectf.Height);

        public static explicit operator Rectf(in Rectangle rectangle)
            => new Rectf(new Vector2(rectangle.X, rectangle.Y),
                         new Vector2(rectangle.Width, rectangle.Height));
    }
}