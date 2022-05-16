using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;

namespace Fracture.Engine.Core.Primitives
{
    /// <summary>
    /// Structure defining a transform defined by position, rotation and scale. Transform also
    /// contains utilities for creating games using unit space instead of pixel space.
    /// </summary>
    public readonly struct Transform
    {
        #region Constant fields
        public const float DefaultScreenUnitToWorldUnitRatio = 32.0f;
        #endregion

        #region Static properties
        /// <summary>
        /// Gets the default transform object that has its position
        /// set to zero (0, 0), scale to one (1, 1) and rotation to zero.
        /// </summary>
        public static Transform Default => new Transform(Vector2.Zero, Vector2.One, 0.0f);

        public static float ScreenUnitsPerWorldUnit
        {
            get;
            private set;
        } = DefaultScreenUnitToWorldUnitRatio;

        public static float ScreenUnitsToWorldUnitsRatio
        {
            get;
            private set;
        } = ScreenUnitsPerWorldUnit;

        public static float WorldUnitsToScreenUnitsRatio
        {
            get;
            private set;
        } = 1.0f / ScreenUnitsPerWorldUnit;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the transforms position.
        /// </summary>
        public Vector2 Position
        {
            get;
        }

        /// <summary>
        /// Gets the transforms scale.
        /// </summary>
        public Vector2 Scale
        {
            get;
        }

        /// <summary>
        /// Gets the transforms rotation in radians.
        /// </summary>
        public float Rotation
        {
            get;
        }
        #endregion

        public Transform(in Vector2 position, in Vector2 scale, float rotation)
        {
            Position = position;
            Scale    = scale;
            Rotation = rotation;
        }

        public Transform(in Vector2 position, in Vector2 scale)
            : this(position, scale, 0.0f)
        {
        }

        public Transform(in Vector2 position, float rotation)
            : this(position, Vector2.One, rotation)
        {
        }

        public Transform(in Vector2 position)
            : this(position, Vector2.One, 0.0f)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Transform ComponentPosition(in Vector2 position) => new Transform(position, Vector2.Zero);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Transform ComponentScale(in Vector2 scale) => new Transform(Vector2.Zero, scale);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Transform ComponentRotation(float rotation) => new Transform(Vector2.Zero, Vector2.Zero, rotation);

        #region Translate methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Transform TranslatePosition(in Transform transform, in Vector2 translation) =>
            new Transform(transform.Position + translation, transform.Scale, transform.Rotation);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Transform TranslateScale(in Transform transform, in Vector2 translation) =>
            new Transform(transform.Position, transform.Scale + translation, transform.Rotation);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Transform TranslateRotation(in Transform transform, float translation) =>
            new Transform(transform.Position, transform.Scale, transform.Rotation + translation);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Transform TranslateLocal(in Transform global, in Transform local) =>
            new Transform(global.Position + local.Position, LocalScale(global, local), global.Rotation + local.Rotation);
        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 LocalScale(in Transform global, in Transform local) => global.Scale * local.Scale;

        #region Transform methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Transform TransformPosition(in Transform transform, in Vector2 transformation) =>
            new Transform(transformation, transform.Scale, transform.Rotation);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Transform TransformScale(in Transform transform, in Vector2 transformation) =>
            new Transform(transform.Position, transformation, transform.Rotation);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Transform TransformRotation(in Transform transform, float transformation) =>
            new Transform(transform.Position, transform.Scale, transformation);
        #endregion

        #region Operators
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Transform operator +(in Transform a, in Transform b) =>
            new Transform(a.Position + b.Position, a.Scale + b.Scale, a.Rotation + b.Rotation);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Transform operator -(in Transform a, in Transform b) =>
            new Transform(a.Position - b.Position, a.Scale - b.Scale, a.Rotation - b.Rotation);
        #endregion

        #region Unit conversion members
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetScreenUnitToWorldUnitRatio(float screenUnitsPerWorldUnit)
        {
            ScreenUnitsPerWorldUnit = screenUnitsPerWorldUnit;

            ScreenUnitsToWorldUnitsRatio = screenUnitsPerWorldUnit;
            WorldUnitsToScreenUnitsRatio = 1.0f / screenUnitsPerWorldUnit;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToScreenUnits(float worldUnits) => worldUnits * ScreenUnitsToWorldUnitsRatio;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToScreenUnits(int worldUnits) => worldUnits * ScreenUnitsToWorldUnitsRatio;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ToScreenUnits(in Vector2 worldUnits) => worldUnits * ScreenUnitsToWorldUnitsRatio;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ToScreenUnits(ref Vector2 worldUnits, out Vector2 screenUnits) =>
            Vector2.Multiply(ref worldUnits, ScreenUnitsToWorldUnitsRatio, out screenUnits);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ToScreenUnits(in Vector3 worldUnits) => worldUnits * ScreenUnitsToWorldUnitsRatio;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ToScreenUnits(float x, float y) => new Vector2(x, y) * ScreenUnitsToWorldUnitsRatio;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ToScreenUnits(float x, float y, out Vector2 screenUnits)
        {
            screenUnits   = Vector2.Zero;
            screenUnits.X = x * ScreenUnitsToWorldUnitsRatio;
            screenUnits.Y = y * ScreenUnitsToWorldUnitsRatio;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToWorldUnits(float screenUnits) => screenUnits * WorldUnitsToScreenUnitsRatio;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToWorldUnits(int screenUnits) => screenUnits * WorldUnitsToScreenUnitsRatio;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ToWorldUnits(in Vector2 screenUnits) => screenUnits * WorldUnitsToScreenUnitsRatio;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ToWorldUnits(ref Vector2 screenUnits, out Vector2 worldUnits) =>
            Vector2.Multiply(ref screenUnits, WorldUnitsToScreenUnitsRatio, out worldUnits);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ToWorldUnits(float x, float y) => new Vector2(x, y) * WorldUnitsToScreenUnitsRatio;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ToWorldUnits(float x, float y, out Vector2 worldUnits)
        {
            worldUnits   = Vector2.Zero;
            worldUnits.X = x * WorldUnitsToScreenUnitsRatio;
            worldUnits.Y = y * WorldUnitsToScreenUnitsRatio;
        }
        #endregion
    }
}