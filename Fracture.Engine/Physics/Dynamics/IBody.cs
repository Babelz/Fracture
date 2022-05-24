using Fracture.Engine.Core.Primitives;
using Microsoft.Xna.Framework;

namespace Fracture.Engine.Physics.Dynamics
{
    /// <summary>
    /// Interface for implementing bodies.
    /// </summary>
    public interface IBody
    {
        #region Properties
        /// <summary>
        /// Current angle of this body in radians.
        /// </summary>
        float Angle
        {
            get;
        }
        /// <summary>
        /// Angle this body will transform to 
        /// during next frame.
        /// </summary>
        float AngleTransform
        {
            get;
        }
        /// <summary>
        /// Translation angle of the body. If translation is being
        /// applied to the body during update call, it will be normalized.
        /// </summary>  
        float AngleTranslation
        {
            get;
        }

        /// <summary>
        /// Position of this body.
        /// </summary>
        Vector2 Position
        {
            get;
        }
        /// <summary>
        /// Position this body will transform to
        /// during this frame.
        /// </summary>
        Vector2 PositionTransform
        {
            get;
        }
        /// <summary>
        /// Position translation of the body. If translation is being 
        /// applied to the body, it will be normalized during update call.
        /// </summary>
        Vector2 PositionTranslation
        {
            get;
        }

        /// <summary>
        /// Custom user specified data to group the body to 
        /// a certain group. Does not have effect from the
        /// point of collision detection.
        /// </summary>
        object Group
        {
            get;
            set;
        }

        /// <summary>
        /// Shape data associated with this body.
        /// </summary>
        Shape Shape
        {
            get;
        }

        /// <summary>
        /// Type of this body.
        /// </summary>
        BodyType Type
        {
            get;
        }

        /// <summary>
        /// Custom user specified data associated with the body.
        /// </summary>
        object UserData
        {
            get;
            set;
        }

        /// <summary>
        /// Current bounding box of the body.
        /// </summary>
        Aabb BoundingBox
        {
            get;
        }
        /// <summary>
        /// Bounding box that contains translation or transformation. In case
        /// no transformation or translation is currently not being applied to the
        /// body, the current bounding box will be returned.
        /// </summary>
        Aabb TransformBoundingBox
        {
            get;
        }

        /// <summary>
        /// Returns boolean declaring is there a transform or
        /// translation being applies to the body in current time.
        /// </summary>
        bool Active
        {
            get;
        }
        /// <summary>
        /// Returns boolean declaring whether this shape will
        /// transform its position and rotation during this frame.
        /// </summary>
        bool Transforming
        {
            get;
        }
        /// <summary>
        /// Returns boolean declaring whether this shape will
        /// translate its position and rotation during this frame.
        /// </summary>
        bool Translating
        {
            get;
        }
        #endregion

        /// <summary>
        /// Saves given transformation for the body to transform.
        /// </summary>
        void Transform(Vector2 positionTransform, float angleTransform);

        /// <summary>
        /// Saves given translation for the body to apply.
        /// </summary>
        void Translate(float translation);

        /// <summary>
        /// Saves given translation for the body to apply.
        /// </summary>
        void Translate(Vector2 translation);

        void Translate(Vector2 position, float angle);
    }
}