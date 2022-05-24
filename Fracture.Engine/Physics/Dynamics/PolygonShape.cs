using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Fracture.Engine.Core.Primitives;
using Microsoft.Xna.Framework;

namespace Fracture.Engine.Physics.Dynamics
{
    public sealed class PolygonShape : Shape
    {
        #region Constant fields
        /// <summary>
        /// Minimum count of vertices required by polygons.
        /// </summary>
        public const int MinVertices = 3;
        #endregion

        #region Fields
        private Vector2[] axes;
        #endregion

        #region Properties
        /// <summary>
        /// Axes of the polygon.
        /// </summary>
        public IReadOnlyList<Vector2> Axes
            => axes;
        #endregion

        public PolygonShape()
            : base(ShapeType.Polygon)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ComputeAxes(Vector2[] points, Vector2[] axes)
        {
            for (var i = 0; i < points.Length; i++)
            {
                // Calculate the edge between each point and its neighbor.
                var edge = points[i] - points[(i + 1) % points.Length];

                edge.Normalize();

                // Store perpendicular vector.
                axes[i].X = edge.Y;
                axes[i].Y = -edge.X;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsConvex(Vector2[] points)
        {
            if (points.Length < MinVertices)
                throw new InvalidOperationException($"expecting {MinVertices} vertices");

            if (points.Length <= MinVertices + 1)
                return true;

            var sign = false;

            for (var i = 0; i < points.Length; i++)
            {
                var dx1 = points[(i + 2) % points.Length].X - points[(i + 1) % points.Length].X;
                var dy1 = points[(i + 2) % points.Length].Y - points[(i + 1) % points.Length].Y;

                var dx2 = points[i].X - points[(i + 1) % points.Length].X;
                var dy2 = points[i].Y - points[(i + 1) % points.Length].Y;

                var zcross = dx1 * dy2 - dy1 * dx2;

                if (i == 0)
                    sign = zcross > 0.0f;
                else if (sign != (zcross > 0.0f))
                    return false;
            }

            return true;
        }

        public void Setup(Vector2[] vertices)
        {
            if (vertices.Length < MinVertices)
                throw new InvalidOperationException($"expecting at least {MinVertices} vertices for polygon");

            if (!IsConvex(vertices))
                throw new InvalidOperationException($"non-convex polygons are not supported");

            // Compute initial AABB.
            BoundingBox = new Aabb(Vector2.Zero, 0.0f, vertices);

            // Compute axes.
            axes = new Vector2[vertices.Length];

            ComputeAxes(vertices, axes);

            // Copy vertex data.
            Setup(vertices, true);
        }

        public override Aabb TranslateBoundingBox(Vector2 positionTranslation,
                                                  float angleTranslation,
                                                  float angle)
        {
            if (angleTranslation != 0.0f)
                return new Aabb(BoundingBox.Position + positionTranslation, angle + angleTranslation, worldVertices);

            return new Aabb(BoundingBox.Position + positionTranslation, BoundingBox.HalfBounds);
        }

        public override Aabb TransformBoundingBox(Vector2 positionTransform,
                                                  float angleTransformation)
        {
            if (angleTransformation != 0.0f)
                return new Aabb(positionTransform, angleTransformation, worldVertices);

            return new Aabb(positionTransform, BoundingBox.HalfBounds);
        }

        public override void ApplyTransformation(Vector2 positionTransform, float angleTransform)
        {
            // Translate position with rotation.
            var sin = (float)Math.Sin(angleTransform);
            var cos = (float)Math.Cos(angleTransform);
                
            for (var i = 0; i < localVertices.Length; i++)
            {
                var rotation = new Vector2(cos * localVertices[i].X - sin * localVertices[i].Y,
                                           sin * localVertices[i].X + cos * localVertices[i].Y);

                worldVertices[i] = rotation + positionTransform;
            }

            // Recompute bounding box.
            BoundingBox = TransformBoundingBox(positionTransform, angleTransform);
        }

        public override Shape Clone()
        {
            var shape = new PolygonShape();

            // Omit position and rotation data.
            shape.Setup(localVertices);

            return shape;   
        }
    }
}
