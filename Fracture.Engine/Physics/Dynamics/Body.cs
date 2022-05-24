using System;
using System.Diagnostics;
using Fracture.Common;
using Fracture.Engine.Core.Primitives;
using Microsoft.Xna.Framework;

namespace Fracture.Engine.Physics.Dynamics
{
    /// <summary>
    /// Class representing a body that are is used to detect collisions with other bodies. 
    /// Bodies are defined by the translation and transform being applied to them and their geometry.
    /// </summary>
    [DebuggerDisplay("UserData = {UserData}")]
    public sealed class Body
    {
        #region Properties
        /// <summary>
        /// Position this body will transform to
        /// during this frame.
        /// </summary>
        public Vector2 PositionTransform
        {
            get;
            private set;
        }

        /// <summary>
        /// Position translation of the body. If translation is being 
        /// applied to the body, it will be normalized during update call.
        /// </summary>
        public Vector2 PositionTranslation
        {
            get;
            private set;
        }

        /// <summary>
        /// Position of this body.
        /// </summary>
        public Vector2 Position
        {
            get;
            private set;
        }

        /// <summary>
        /// Angle this body will transform to 
        /// during next frame.
        /// </summary>
        public float AngleTransform
        {
            get;
            private set;
        }

        /// <summary>
        /// Translation angle of the body. If translation is being
        /// applied to the body during update call, it will be normalized.
        /// </summary>  
        public float AngleTranslation
        {
            get;
            private set;
        }

        /// <summary>
        /// Current angle of this body in radians.
        /// </summary>
        public float Angle
        {
            get;
            private set;
        }

        /// <summary>
        /// Type of this body.
        /// </summary>
        public BodyType Type
        {
            get;
            private set;
        }

        /// <summary>
        /// Custom user specified data associated with the body.
        /// </summary>
        public object UserData
        {
            get;
            set;
        }

        /// <summary>
        /// Custom user specified data to group the body to 
        /// a certain group. Does not have effect from the
        /// point of collision detection.
        /// </summary>
        public object Group
        {
            get;
            set;
        }

        /// <summary>
        /// Shape data associated with this body.
        /// </summary>
        public Shape Shape
        {
            get;
            private set;
        }

        /// <summary>
        /// Returns boolean declaring whether this shape will
        /// transform its position and rotation during this frame.
        /// </summary>
        public bool Transforming
        {
            get;
            private set;
        }

        /// <summary>
        /// Returns boolean declaring whether this shape will
        /// translate its position and rotation during this frame.
        /// </summary>
        public bool Translating
        {
            get;
            private set;
        }

        /// <summary>
        /// Returns boolean declaring is there a transform or
        /// translation being applies to the body in current time.
        /// </summary>
        public bool Active
            => Translating || Transforming;

        public bool Normalized
        {
            get;
            private set;
        }

        /// <summary>
        /// Current bounding box of the body.
        /// </summary>
        public Aabb BoundingBox
            => Shape.BoundingBox;

        /// <summary>
        /// Bounding box that contains translation or transformation. In case
        /// no transformation or translation is currently not being applied to the
        /// body, the current bounding box will be returned.
        /// </summary>
        public Aabb TransformBoundingBox
        {
            get;
            private set;
        }
        #endregion

        public Body()
        {
        }
        
        public void Setup(BodyType type, Shape shape, Vector2 position, float angle = 0.0f)
        {
            Type  = type != BodyType.None ? type : throw new InvalidOrUnsupportedException(nameof(type), type);
            Shape = shape ?? throw new ArgumentNullException(nameof(shape));

            UserData = null;
            Group    = null;

            // No need to transform, just keep track of the initial state.
            Position = position;
            Angle    = angle;

            AngleTransform   = 0.0f;
            AngleTranslation = 0.0f;

            PositionTransform   = Vector2.Zero;
            PositionTranslation = Vector2.Zero;

            // Apply initial shape transform.
            shape.ApplyTransformation(position, angle);

            // In case user does not translate or transform the body immediately after it has been
            // created, apply zero translation for solver to notice us.
            if (type == BodyType.Dynamic)
                Translate(Vector2.Zero);
            else
                TransformBoundingBox = BoundingBox;
        }
        
        private void ResetTranslation()
        {
            if (!Translating) return;
            
            Normalized           = false;
            Translating          = false;
            AngleTranslation     = 0.0f;
            PositionTranslation  = Vector2.Zero;
            TransformBoundingBox = BoundingBox;
        }

        private void ResetTransform()
        {
            if (!Transforming) return;
            
            Normalized           = false;
            Transforming         = false;
            AngleTransform       = 0.0f;
            PositionTransform    = Vector2.Zero;
            TransformBoundingBox = BoundingBox;
        }

        public void Translate(Vector2 translation)
        {
            // Static bodies can't be moved or rotated.
            if (Type == BodyType.Static)
                return;
            
            ResetTransform();

            Translating          = true;
            PositionTranslation  = translation;
            TransformBoundingBox = Shape.TranslateBoundingBox(translation, AngleTranslation, Angle); 
        }

        /// <summary>
        /// Saves given translation for the body to apply 
        /// during next <see cref="ApplyTransformation"/> call.
        /// </summary>
        public void Translate(float translation)
        {
            // Static bodies can't be moved or rotated.
            if (Type == BodyType.Static)
                return;
            
            ResetTransform();

            Translating          = true;
            AngleTranslation     = translation;
            TransformBoundingBox = Shape.TranslateBoundingBox(PositionTranslation, translation, Angle);
        }
        
        public void Translate(Vector2 position, float angle)
        {
            Translate(position);

            Translate(angle);
        }
        
        /// <summary>
        /// Saves given transformation for the body to transform to
        /// during next <see cref="ApplyTransformation"/> call.
        /// </summary>
        public void Transform(Vector2 positionTransform, float angleTransform)
        {
            // Static bodies can't be moved or rotated.
            if (Type == BodyType.Static)
                return;
            
            ResetTranslation();

            Transforming         = true;
            PositionTransform    = positionTransform;
            AngleTransform       = angleTransform;
            TransformBoundingBox = Shape.TransformBoundingBox(positionTransform, angleTransform); 
        }

        public void Transform(Vector2 positionTransform)
            => Transform(positionTransform, AngleTransform);

        public void Transform(float angleTransform)
            => Transform(PositionTransform, angleTransform);

        public void NormalizeTransformation(float delta = 1.0f)
        {
            if (!Translating) return;
            if (Normalized)   return;

            Normalized           = true;
            PositionTranslation *= delta;
            AngleTranslation    *= delta;
            TransformBoundingBox = Shape.TranslateBoundingBox(PositionTranslation, AngleTranslation, Angle);
        }

        /// <summary>
        /// Applies translation or transformation to the body and 
        /// it's shape. 
        /// </summary>
        public void ApplyTransformation()
        {
            // Static bodies can't be moved or rotated.
            if (Type == BodyType.Static)
                return;

            if (Translating)
            {
                // Store current angle before translation for later checks.
                // In case the angle does not change, we can skip transforming 
                // the shape and just translate it's geometry to our current translation
                // vector.
                var oldAngle = Angle;

                Position += PositionTranslation;
                Angle    += AngleTranslation;

                // No need to transform the position in case angle did not change, we 
                // can just translate.
                if (oldAngle != Angle) Shape.ApplyTransformation(Position, Angle);
                else                   Shape.ApplyTranslation(PositionTranslation);
                
                ResetTranslation();
            }
            else if (Transforming)
            {
                Position = PositionTransform;
                Angle    = AngleTransform;

                // Always transform shape in case of 
                // transform is preferred.
                Shape.ApplyTransformation(Position, Angle);

                ResetTransform();
            }
        }
    }
}
