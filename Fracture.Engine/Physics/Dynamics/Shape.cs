using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;

namespace Fracture.Engine.Physics.Dynamics
{
    /// <summary>
    /// Structure that defines shape data.
    /// </summary>
    public readonly struct Shape
    {
        #region Properties
        /// <summary>
        /// Gets the shape vertices. These vertices are
        /// in local shape space and should never be modified.
        /// </summary>
        public readonly Vector2 [] Vertices;

        /// <summary>
        /// Gets the shape axes. These vertices
        /// should never be modified. All shapes
        /// don't have axes.
        /// </summary>
        public readonly Vector2 [] Axes;
        #endregion

        private Shape(Vector2 [] vertices, Vector2 [] axes)
        {
            Vertices = vertices ?? throw new ArgumentNullException(nameof(vertices));
            Axes     = axes;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector2 [] ComputeAxes(Vector2 [] vertices)
        {
            var axes = new Vector2[vertices.Length];

            for (var i = 0; i < vertices.Length; i++)
            {
                // Calculate the edge between each point and its neighbor.
                var edge = vertices[i] - vertices[(i + 1) % vertices.Length];

                edge.Normalize();

                // Store perpendicular vector.
                axes[i].X = edge.Y;
                axes[i].Y = -edge.X;
            }

            return axes;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsConvex(Vector2 [] vertices)
        {
            const int ConvexMinVertices = 3;

            if (vertices.Length < ConvexMinVertices)
                throw new InvalidOperationException($"expecting {ConvexMinVertices} vertices");

            if (vertices.Length <= ConvexMinVertices + 1)
                return true;

            var sign = false;

            for (var i = 0; i < vertices.Length; i++)
            {
                var dx1 = vertices[(i + 2) % vertices.Length].X - vertices[(i + 1) % vertices.Length].X;
                var dy1 = vertices[(i + 2) % vertices.Length].Y - vertices[(i + 1) % vertices.Length].Y;

                var dx2 = vertices[i].X - vertices[(i + 1) % vertices.Length].X;
                var dy2 = vertices[i].Y - vertices[(i + 1) % vertices.Length].Y;

                var zCross = dx1 * dy2 - dy1 * dx2;

                if (i == 0)
                    sign = zCross > 0.0f;
                else if (sign != (zCross > 0.0f))
                    return false;
            }

            return true;
        }

        public bool IsCircle()
            => Vertices.Length == 2;

        public bool IsPolygon()
            => Vertices.Length >= 3;

        public bool IsBox()
            => Vertices.Length == 4;

        public float Radius()
            => Vertices[1].X - Vertices[0].X;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Shape CreateCircle(float radius)
            => new Shape(new [] { Vector2.Zero, new Vector2(radius) }, null);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Shape CreateBox(Vector2 bounds)
        {
            var vertices = new []
            {
                // Top left.
                new Vector2(bounds.X * -0.5f, bounds.Y * -0.5f),

                // Top right.
                new Vector2(bounds.X * 0.5f, bounds.Y * -0.5f),

                // Bottom right.
                new Vector2(bounds.X * 0.5f, bounds.Y * 0.5f),

                // Bottom left.
                new Vector2(bounds.X * -0.5f, bounds.Y * 0.5f),
            };

            return new Shape(vertices, ComputeAxes(vertices));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Shape CreatePolygon(Vector2 [] vertices)
        {
            if (!IsConvex(vertices))
                throw new InvalidOperationException("non-convex polygons are not supported");

            var shape = new Shape(vertices, ComputeAxes(vertices));

            if (!shape.IsPolygon())
                throw new InvalidOperationException("vertices do not create polygon");

            return shape;
        }
    }

    public static class Project
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Polygon(Vector2 [] vertices, in Vector2 axis)
        {
            // Set min and max to the first point for a base case.
            var projection = new Vector2(Vector2.Dot(axis, vertices[0]));

            for (var i = 1; i < vertices.Length; i++)
            {
                var next = Vector2.Dot(axis, vertices[i]);

                if (next < projection.X) projection.X      = next;
                else if (next > projection.Y) projection.Y = next;
            }

            return projection;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Circle(float radius, Vector2 [] vertices, in Vector2 axis)
        {
            var projection = Vector2.Dot(axis, vertices[0]);

            return new Vector2(projection - radius, projection + radius);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Overlap(in Vector2 a, in Vector2 b)
            => (a.Y >= b.X) || (a.X >= b.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float OverlapAmount(in Vector2 a, in Vector2 b)
            => Math.Min(a.Y, b.Y) - Math.Max(a.X, b.X);
    }
}