using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Fracture.Common.Memory;
using Fracture.Engine.Core.Primitives;
using Microsoft.Xna.Framework;

namespace Fracture.Engine.Physics.Dynamics
{
    /// <summary>
    /// Enumeration that defines supported shape types. This is a hint
    /// about the shapes type and is used to speed up solver lookups to
    /// avoid reflection.
    /// </summary>
    public enum ShapeType : byte
    {
        /// <summary>
        /// Undefined shape type.
        /// </summary>
        None = 0,

        /// <summary>
        /// Circle shape with 1 vertex.
        /// </summary>
        Circle = 1,

        /// <summary>
        /// Polygon shape with n-vertices.
        /// </summary>
        Polygon = 2,
    }

    /// <summary>
    /// Interface that represents shapes that are defined by their type, vertices, transformations and translations.
    /// Shapes do not have position, rotation or bounding data associated with them and they are only used
    /// to contain body transformed and local geometry data. Shapes contain data at any given point
    /// in time and the data will be updated when the owning body position or rotation changes.
    /// </summary>
    public interface IShape : ICloneable<IShape>
    {
        #region Properties
        /// <summary>
        /// Vertices in local space.
        /// </summary>
        IReadOnlyList<Vector2> LocalVertices
        {
            get;
        }

        /// <summary>
        /// Vertices in world space.
        /// </summary>
        IReadOnlyList<Vector2> WorldVertices
        {
            get;
        }

        /// <summary>
        /// Axes of the the shape.
        /// </summary>
        IReadOnlyList<Vector2> Axes
        {
            get;
        }

        /// <summary>
        /// Type of the shape.
        /// </summary>
        ShapeType Type
        {
            get;
        }

        /// <summary>
        /// Returns current bounding box of the shape.
        /// </summary>
        Aabb BoundingBox
        {
            get;
        }
        #endregion

        /// <summary>
        /// Computes AABB for the shape after given translation.
        /// </summary>
        Aabb TranslateBoundingBox(in Vector2 positionTranslation, float angleTranslation, float angle);

        /// <summary>
        /// Computes AABB for the shape after given transformation.
        /// </summary>
        Aabb TransformBoundingBox(in Vector2 positionTransform, float angleTransformation);

        /// <summary>
        /// Immediately applies given position and angle transformation to the shapes
        /// geometry data.
        /// </summary>
        void ApplyTransformation(in Vector2 positionTransform, float angleTransform);

        /// <summary>
        /// Immediately applies given position translation to the shapes
        /// geometry data with no rotation included. Faster than full transformation
        /// that recomputes all geometry data.
        /// </summary>
        void ApplyTranslation(in Vector2 translation);
    }

    public struct CircleShape : IShape
    {
        #region Fields
        private readonly Vector2[] localVertices;
        private readonly Vector2[] worldVertices;

        private readonly float radius;
        #endregion

        #region Properties
        public IReadOnlyList<Vector2> LocalVertices => localVertices;

        public IReadOnlyList<Vector2> WorldVertices => worldVertices;

        public readonly IReadOnlyList<Vector2> Axes => Array.Empty<Vector2>();

        public ShapeType Type => ShapeType.Circle;

        public Aabb BoundingBox
        {
            get;
            private set;
        }
        #endregion

        public CircleShape(float radius)
            : this()
        {
            this.radius = radius > 0.0f ? radius : throw new ArgumentOutOfRangeException(nameof(radius));

            BoundingBox = new Aabb(Vector2.Zero, new Vector2(radius));

            // Setup AABB and vertices.
            localVertices = new[]
            {
                Vector2.Zero,
            };

            worldVertices = new[]
            {
                Vector2.Zero,
            };
        }

        public Aabb TranslateBoundingBox(in Vector2 positionTranslation, float angleTranslation, float angle)
            // Circles are not affected by rotation.
            => new Aabb(BoundingBox.Position + positionTranslation, new Vector2(radius));

        public Aabb TransformBoundingBox(in Vector2 positionTransform, float angleTransformation)
            // Circles are not affected by rotation.
            => new Aabb(positionTransform, new Vector2(radius));

        public void ApplyTransformation(in Vector2 positionTransform, float angleTransform)
        {
            // Circles are not affected by rotation.
            worldVertices[0] = positionTransform;

            // Translate box.
            BoundingBox = new Aabb(positionTransform, BoundingBox.HalfBounds);
        }

        public void ApplyTranslation(in Vector2 translation)
        {
            // Translate world vertices.
            for (var i = 0; i < worldVertices.Length; i++)
                worldVertices[i] += translation;

            // Translate box.
            BoundingBox = new Aabb(BoundingBox.Position + translation, BoundingBox.HalfBounds);
        }

        public IShape Clone()
            => new CircleShape(radius);
    }

    public struct PolygonShape : IShape
    {
        #region Constant fields
        /// <summary>
        /// Minimum count of vertices required by polygons.
        /// </summary>
        public const int MinVertices = 3;
        #endregion

        #region Fields
        private readonly Vector2[] localVertices;
        private readonly Vector2[] worldVertices;

        private readonly Vector2[] axes;
        #endregion

        #region Properties
        public IReadOnlyList<Vector2> Axes => axes;

        public IReadOnlyList<Vector2> LocalVertices => localVertices;

        public IReadOnlyList<Vector2> WorldVertices => worldVertices;

        public ShapeType Type => ShapeType.Polygon;

        public Aabb BoundingBox
        {
            get;
            private set;
        }
        #endregion

        public PolygonShape(Vector2[] vertices)
        {
            if (vertices.Length < MinVertices)
                throw new InvalidOperationException($"expecting at least {MinVertices} vertices for polygon");

            if (!IsConvex(vertices))
                throw new InvalidOperationException($"non-convex polygons are not supported");

            localVertices = vertices.ToArray();
            worldVertices = vertices.ToArray();

            // Compute initial AABB.
            BoundingBox = new Aabb(Vector2.Zero, 0.0f, vertices);

            // Compute axes.
            axes = new Vector2[vertices.Length];

            ComputeAxes(vertices, axes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ComputeAxes(Vector2[] vertices, Vector2[] axes)
        {
            for (var i = 0; i < vertices.Length; i++)
            {
                // Calculate the edge between each point and its neighbor.
                var edge = vertices[i] - vertices[(i + 1) % vertices.Length];

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
                else if (sign != zcross > 0.0f)
                    return false;
            }

            return true;
        }

        public Aabb TranslateBoundingBox(in Vector2 positionTranslation, float angleTranslation, float angle)
        {
            if (angleTranslation != 0.0f)
                return new Aabb(BoundingBox.Position + positionTranslation, angle + angleTranslation, worldVertices);

            return new Aabb(BoundingBox.Position + positionTranslation, BoundingBox.HalfBounds);
        }

        public Aabb TransformBoundingBox(in Vector2 positionTransform, float angleTransformation)
        {
            if (angleTransformation != 0.0f)
                return new Aabb(positionTransform, angleTransformation, worldVertices);

            return new Aabb(positionTransform, BoundingBox.HalfBounds);
        }

        public void ApplyTransformation(in Vector2 positionTransform, float angleTransform)
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

        public void ApplyTranslation(in Vector2 translation)
        {
            // Translate world vertices.
            for (var i = 0; i < worldVertices.Length; i++)
                worldVertices[i] += translation;

            // Translate box.
            BoundingBox = new Aabb(BoundingBox.Position + translation, BoundingBox.HalfBounds);
        }

        public IShape Clone()
            => new PolygonShape(localVertices);
    }

    public static class ShapeFactory
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IShape CreateCircle(float radius)
            => new CircleShape(radius);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IShape CreateBox(Vector2 bounds)
            => new PolygonShape(new[]
            {
                // Top left.
                new Vector2(bounds.X * -0.5f, bounds.Y * -0.5f),

                // Top right.
                new Vector2(bounds.X * 0.5f, bounds.Y * -0.5f),

                // Bottom right.
                new Vector2(bounds.X * 0.5f, bounds.Y * 0.5f),

                // Bottom left.
                new Vector2(bounds.X * -0.5f, bounds.Y * 0.5f),
            });

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IShape CreatePolygon(Vector2[] vertices)
            => new PolygonShape(vertices);
    }
}