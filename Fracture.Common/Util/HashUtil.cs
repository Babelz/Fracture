using System.Runtime.CompilerServices;

namespace Fracture.Common.Util
{
    /// <summary>
    /// Utility class for creating hash codes for objects.
    /// </summary>
    public static class HashUtils
    {
        /// <summary>
        /// Creates new base value for hashing.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Create()
        {
            unchecked
            {
                return (int)2166136261;
            }
        }

        /// <summary>
        /// Appends given value to hash.
        /// </summary>
        /// <param name="hash">current hash</param>
        /// <param name="value">value to be appended</param>
        /// <returns>hash value that has value appended</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Append<T>(this int hash, T value)
        {
            // Overflow is fine, just wrap.
            unchecked
            {
                hash = (hash * 16777619) ^ value?.GetHashCode() ?? 0;

                return hash;
            }
        }

        /// <summary>
        /// Appends given array of values to hash.
        /// </summary>
        /// <param name="hash">current hash</param>
        /// <param name="value">values to be appended</param>
        /// <returns>hash value that has values appended</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Append<T>(this int hash, T[] value)
        {
            if (value == null)
                return hash;

            for (int i = 0, length = value.Length; i < length; i++)
                hash = hash.Append(value[i].GetHashCode());

            return hash;
        }
    }
}