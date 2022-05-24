using System;
using Fracture.Engine.Core.Primitives;
using Microsoft.Xna.Framework;

namespace Fracture.Engine.Physics.Dynamics
{
    public sealed class CircleShape : Shape
    {
        #region Constant fields
        /// <summary>
        /// Count of vertices required by circles.
        /// </summary>
        public const int VerticesCount = 1;
        #endregion

        #region Properties
        public float Radius
        {
            get;
            private set;
        }
        #endregion

        public CircleShape()
            : base(ShapeType.Circle)
        {
        }

        public void Setup(float radius)
        {
            Radius      = radius > 0.0f ? radius : throw new ArgumentOutOfRangeException(nameof(radius));
            BoundingBox = new Aabb(Vector2.Zero, new Vector2(radius));

            // Setup AABB and vertices.
            var vertices = new Vector2[VerticesCount]
            {
                Vector2.Zero
            };

            // Setup base shape.
            Setup(vertices, false);
        }

        public override Aabb TranslateBoundingBox(Vector2 positionTranslation,
                                                  float angleTranslation,
                                                  float angle)
        {
            // Circles are not affected by rotation.
            return new Aabb(BoundingBox.Position + positionTranslation, new Vector2(Radius));
        }

        public override Aabb TransformBoundingBox(Vector2 positionTransform,
                                                  float angleTransformation)
        {
            // Circles are not affected by rotation.
            return new Aabb(positionTransform, new Vector2(Radius));
        }

        public override void ApplyTransformation(Vector2 positionTransform, float angleTransform)
        {
            // Circles are not affected by rotation.
            worldVertices[0] = positionTransform;

            // Translate box.
            BoundingBox = new Aabb(positionTransform, BoundingBox.HalfBounds);
        }

        public override Shape Clone()
        {
            var shape = new CircleShape();

            // Omit position and rotation data.
            shape.Setup(Radius);

            return shape;
        }
    }
}
