using System;
using System.Runtime.CompilerServices;

namespace Fracture.Common.Util
{
    /// <summary>
    /// Static utility class containing common math functions.
    /// </summary>
    public static class MathUtil
    {
        #region Constant fields
        public const float FloatPrecision = 0.01f;

        public const double DoublePrecision = 0.01d;
        #endregion

        /// <summary>
        /// Returns next closes to power of two value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int NextPowerOfTwo(int value)
        {
            value--;

            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;

            value++;

            return value;
        }

        /// <summary>
        /// Check whether two float values are nearly equal with given precision.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool NearlyEqual(float a, float b, float precision = FloatPrecision)
            => Math.Abs(a - b) <= precision;

        /// <summary>
        /// Check whether two double values are nearly equal with given precision.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool NearlyEqual(double a, double b, double precision = DoublePrecision)
            => Math.Abs(a - b) <= precision;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Clamp<T>(T value, T min, T max) where T : IComparable<T>
        {
            if (value.CompareTo(min) < 0) return min;

            return value.CompareTo(max) > 0 ? max : value;
        }
    }
}