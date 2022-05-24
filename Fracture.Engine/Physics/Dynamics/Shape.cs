using System.Collections.Generic;
using System.Linq;
using Fracture.Common;
using Fracture.Common.Memory;
using Fracture.Engine.Core.Primitives;
using Microsoft.Xna.Framework;

namespace Fracture.Engine.Physics.Dynamics
{
    /// <summary>
    /// Abstract class that represents shapes that are defined by their type, vertices, transformations and translations.
    /// Shapes do not have position, rotation or bounding data associated with them and they are only used
    /// to contain bodies transformed and local geometry data. Shapes contain data at any given point
    /// in time and the data will be updated when the owning body position or rotation changes.
    /// </summary>
    public abstract class Shape : ICloneable<Shape>
    {
        #region Fields
        protected Vector2[] localVertices;
        protected Vector2[] worldVertices;
        #endregion

        #region Properties
        /// <summary>
        /// Vertices in local space.
        /// </summary>
        public IReadOnlyList<Vector2> LocalVertices
            => localVertices;

        /// <summary>
        /// Vertices in world space.
        /// </summary>
        public IReadOnlyList<Vector2> WorldVertices
            => worldVertices;

        /// <summary>
        /// Type of the shape.
        /// </summary>
        public ShapeType Type
        {
            get;
        }

        /// <summary>
        /// Returns current bounding box of the shape.
        /// </summary>
        public Aabb BoundingBox
        {
            get;
            protected set;
        }
        #endregion

        protected Shape(ShapeType type)
            => Type = type != ShapeType.None ? type : 
                                               throw new InvalidOrUnsupportedException(nameof(ShapeType), type);

        /// <summary>
        /// Setups the shape and it's vertices either by copying or
        /// taking owner ship of the given vertices.
        /// 
        /// If copy is set to false, shape takes ownership of the
        /// given vertices.
        /// </summary>
        protected void Setup(Vector2[] vertices, bool copy = true)
        {
            if (copy)
            {
                localVertices = vertices.ToArray();
            }
            else
            {
                // Even while we do not copy the vertex data,
                // we need to copy world vertices because we 
                // mutate them.
                localVertices = vertices;
            }

            worldVertices = localVertices.ToArray(); 
        }

        /// <summary>
        /// Computes AABB for the shape after given translation.
        /// </summary>
        public abstract Aabb TranslateBoundingBox(Vector2 positionTranslation, 
                                                  float angleTranslation, 
                                                  float angle);
        
        /// <summary>
        /// Computes AABB for the shape after given transformation.
        /// </summary>
        public abstract Aabb TransformBoundingBox(Vector2 positionTransform, 
                                                  float angleTransformation);
        
        /// <summary>
        /// Immediately applies given position and angle transformation to the shapes
        /// geometry data.
        /// </summary>
        public abstract void ApplyTransformation(Vector2 positionTransform, float angleTransform);

        /// <summary>
        /// Immediately applies given position translation to the shapes
        /// geometry data with no rotation included. Faster than full transformation
        /// that recomputes all geometry data.
        /// </summary>
        public virtual void ApplyTranslation(Vector2 translation)
        {
            // Translate world vertices.
            for (var i = 0; i < worldVertices.Length; i++)
                worldVertices[i] += translation;

            // Translate box.
            BoundingBox = new Aabb(BoundingBox.Position + translation, BoundingBox.HalfBounds);
        }

        /// <summary>
        /// Returns shallow copy of the shape where
        /// global vertex data is omitted.
        /// </summary>
        public abstract Shape Clone();
    }
}
