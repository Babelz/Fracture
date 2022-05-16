using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Fracture.Common.Util;
using Microsoft.Xna.Framework;

namespace Fracture.Engine.Core.Primitives
{
    /// <summary>
    /// Structure that works as a axis aligned bounding box. Bounding boxes are
    /// always positioned around center of the bounds.
    /// </summary>
    public readonly struct Aabb
    {
        #region Constant fields
        private const int PointsLength = 4;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the position of the bounding box.
        /// </summary>
        public Vector2 Position
        {
            get;
        }

        /// <summary>
        /// Gets the bounds of the bounding box.
        /// </summary>
        public Vector2 Bounds
        {
            get;
        }

        /// <summary>
        /// Gets the half bounds of the bounding box.
        /// </summary>
        public Vector2 HalfBounds
        {
            get;
        }

        /// <summary>
        /// Gets the rotation of the bounding box in radians.
        /// </summary>
        public float Rotation
        {
            get;
        }

        /// <summary>
        /// Gets the top side position of the AABB.
        /// </summary>
        public float Top => Position.Y - HalfBounds.Y;

        /// <summary>
        /// Gets the left side position of the AABB.
        /// </summary>
        public float Left => Position.X - HalfBounds.X;

        /// <summary>
        /// Gets the bottom side position of the AABB.
        /// </summary>
        public float Bottom => Position.Y + HalfBounds.Y;

        /// <summary>
        /// Gets the right side position of the AABB.
        /// </summary>
        public float Right => Position.X + HalfBounds.X;
        #endregion

        public Aabb(in Vector2 position, float rotation, in Vector2 halfBounds)
        {
            unsafe
            {
                var points = stackalloc Vector2 [PointsLength];

                Rotate(halfBounds,
                       halfBounds,
                       rotation,
                       out var p1,
                       out var p2,
                       out var p3,
                       out var p4);

                points[0] = p1;
                points[1] = p2;
                points[2] = p3;
                points[3] = p4;

                // Find min and max points.
                var minX = float.MaxValue;
                var maxX = float.MinValue;

                var minY = float.MaxValue;
                var maxY = float.MinValue;

                for (var i = 0; i < PointsLength; i++)
                {
                    var (x, y) = points[i];

                    if (x < minX)
                        minX = x;

                    if (x > maxX)
                        maxX = x;

                    if (y < minY)
                        minY = y;

                    if (y > maxY)
                        maxY = y;
                }

                Bounds     = new Vector2(maxX - minX, maxY - minY);
                HalfBounds = Bounds * 0.5f;
                Position   = position;
                Rotation   = rotation;
            }
        }

        /// <summary>
        /// Creates new AABB with given position and computes its bounds from
        /// given vertices. Assumes the vertices are already rotated.
        /// </summary>
        public Aabb(in Vector2 position, float rotation, Vector2 [] vertices)
        {
            var minX = vertices.Min(v => v.X);
            var minY = vertices.Min(v => v.Y);
            var maxX = vertices.Max(v => v.X);
            var maxY = vertices.Max(v => v.Y);

            var w = maxX - minX;
            var h = maxY - minY;

            Bounds     = new Vector2(w, h);
            HalfBounds = Bounds * 0.5f;
            Position   = position;
            Rotation   = rotation;
        }

        public Aabb(in Vector2 position, in Vector2 halfBounds)
        {
            Position   = position;
            HalfBounds = halfBounds;
            Bounds     = HalfBounds * 2.0f;
            Rotation   = 0.0f;
        }

        public Aabb(in Aabb aabb, in Vector2 position)
        {
            Position   = position;
            Rotation   = aabb.Rotation;
            Bounds     = aabb.Bounds;
            HalfBounds = aabb.HalfBounds;
        }

        public override bool Equals(object obj) => obj is Aabb other && this == other;

        public override int GetHashCode() =>
            HashUtils.Create()
                     .Append(Position)
                     .Append(Bounds)
                     .Append(Rotation);

        public override string ToString() => $"x: {Position.X}, y: {Position.Y}, w: {Bounds.X}, h: {Bounds.Y}, t: {Top}, b: {Bottom}, l: {Left}, r: {Right}";

        /// <summary>
        /// Rotates box vertices around given position and bounds.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Rotate(in Vector2 position,
                                  in Vector2 bounds,
                                  float rotation,
                                  out Vector2 topLeft,
                                  out Vector2 topRight,
                                  out Vector2 bottomLeft,
                                  out Vector2 bottomRight)
        {
            // Rotate points in local space around the center.
            var center = bounds * 0.5f;
            var sin    = (float)Math.Sin(rotation);
            var cos    = (float)Math.Cos(rotation);

            // Top left.
            topLeft.X =  (-center.X * cos) - (-center.Y * sin);
            topLeft.Y =  (-center.X * sin) + (-center.Y * cos);
            topLeft   += position + center;

            // Bottom left.
            bottomLeft.X =  (-center.X * cos) - (center.Y * sin);
            bottomLeft.Y =  (-center.X * sin) + (center.Y * cos);
            bottomLeft   += position + center;

            // Top right.
            topRight.X =  (center.X * cos) - (-center.Y * sin);
            topRight.Y =  (center.X * sin) + (-center.Y * cos);
            topRight   += position + center;

            // Bottom right.
            bottomRight.X =  (center.X * cos) - (center.Y * sin);
            bottomRight.Y =  (center.X * sin) + (center.Y * cos);
            bottomRight   += position + center;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Intersects(in Aabb a, in Aabb b) =>
            (a.Position.X - a.HalfBounds.X) < (b.Position.X + b.HalfBounds.X) &&
            (a.Position.X + a.HalfBounds.X) > (b.Position.X - b.HalfBounds.X) &&
            (a.Position.Y - a.HalfBounds.Y) < (b.Position.Y + b.HalfBounds.Y) &&
            (a.Position.Y + a.HalfBounds.Y) > (b.Position.Y - b.HalfBounds.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Aabb TranslatePosition(in Aabb aabb, in Vector2 translation) => new Aabb(aabb, aabb.Position + translation);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Aabb TransformPosition(in Aabb aabb, in Vector2 transformation) => new Aabb(aabb, in transformation);

        public static bool operator ==(in Aabb lhs, in Aabb rhs) =>
            lhs.Bounds == rhs.Bounds &&
            lhs.Position == rhs.Position &&
            Math.Abs(lhs.Rotation - rhs.Rotation) <= 0.01f;

        public static bool operator !=(in Aabb lhs, in Aabb rhs) => !(lhs == rhs);

        public static explicit operator Rectangle(in Aabb aabb) =>
            new Rectangle((int)(aabb.Position.X - aabb.Bounds.X * 0.5f),
                          (int)(aabb.Position.Y - aabb.Bounds.Y * 0.5f),
                          (int)aabb.Bounds.X,
                          (int)aabb.Bounds.Y);

        public static explicit operator Rectf(in Aabb aabb) => new Rectf(aabb.Position - aabb.Bounds * 0.5f, aabb.Bounds);
    }
}