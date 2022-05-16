using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;

namespace Fracture.Engine.Core.Primitives
{
    /// <summary>
    /// Structure defining a line between two points.
    /// </summary>
    public readonly struct Line
    {
        #region Properties
        /// <summary>
        /// Gets the first point of the line.
        /// </summary>
        public Vector2 A
        {
            get;
        }

        /// <summary>
        /// Gets the second point of the line.
        /// </summary>
        public Vector2 B
        {
            get;
        }
        #endregion

        public Line(in Vector2 a, in Vector2 b)
        {
            A = a;
            B = b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Intersects(in Line a, in Line b)
        {
            var (x1, y1) = a.A;
            var (x2, y2) = a.B;
            var (x3, y3) = b.A;
            var (x4, y4) = b.B;

            var ac = ((x4 - x3) * (y1 - y3) - (y4 - y3) * (x1 - x3)) / ((y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1));
            var bc = ((x2 - x1) * (y1 - y3) - (y2 - y1) * (x1 - x3)) / ((y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1));

            return ac >= 0.0f && ac <= 1.0f &&
                   bc >= 0.0f && bc <= 1.0f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Intersects(in Line line, in Aabb aabb) =>
            Intersects(line, new Line(new Vector2(aabb.Left, aabb.Top), new Vector2(aabb.Right, aabb.Top))) ||
            Intersects(line, new Line(new Vector2(aabb.Right, aabb.Top), new Vector2(aabb.Right, aabb.Bottom))) ||
            Intersects(line, new Line(new Vector2(aabb.Right, aabb.Bottom), new Vector2(aabb.Left, aabb.Bottom))) ||
            Intersects(line, new Line(new Vector2(aabb.Left, aabb.Bottom), new Vector2(aabb.Left, aabb.Top)));
    }
}