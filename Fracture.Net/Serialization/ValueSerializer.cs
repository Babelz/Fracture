using System;
using System.Runtime.CompilerServices;

namespace Fracture.Net.Serialization
{
    /// <summary>
    /// Internal static utility class for holding serialization type id generation context. These values should be
    /// static to type.
    /// </summary>
    internal static class SerializationTypeGenerator
    {
        #region Static fields
        private static ushort next = 0;
        #endregion
        
        /// <summary>
        /// Gets next static serialization type id from the generator.
        /// </summary>
        public static ushort Next() => next++;
    }
    
    /// <summary>
    /// Interface creating type free serializers for serializing single values. These values
    /// can be anything from single primitive to more complex classes and specific types like lists. 
    /// </summary>
    public interface IValueSerializer
    {
        /// <summary>
        /// Serializes given value to given buffer starting at given offset.
        /// </summary>
        void Serialize(object value, byte[] buffer, int offset);
        
        /// <summary>
        /// Deserializes value from given buffer starting at given offset.
        /// </summary>
        object Deserialize(byte[] buffer, int offset); 
        
        /// <summary>
        /// Gets the size of the value from given buffer at given offset.
        /// </summary>
        int GetSizeFromBuffer(byte[] buffer, int offset);
        
        /// <summary>
        /// Returns the size of the object when it is serialized.
        /// </summary>
        int GetSizeFromValue(object value);
    }

    public abstract class ValueSerializer<T> : IValueSerializer
    {
        #region Static properties
        /// <summary>
        /// Gets the runtime type that this value serializer serializes from and to from the serialization buffer. This
        /// value should be static to the class.
        /// </summary>
        public static Type RuntimeType
        {
            get;
        } = typeof(T);
        
        /// <summary>
        /// Gets the runtime type id that this value serializer writes and accepts from the serialization buffer. This
        /// value should be static to the class.
        /// </summary>
        public static ushort SerializationType
        {
            get;
        } = SerializationTypeGenerator.Next();
        #endregion
        
        protected ValueSerializer()
        { 
        }
        
        /// <summary>
        /// Static utility check for doing bounds check on upper bound of a buffer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void CheckUpperBound(int bufferLength, int offset, int size)
        {
            if (offset <= 0) return;
            
            if (offset + size >= bufferLength)
                throw new ArgumentOutOfRangeException($"writing {size} bytes at offset {offset} would overflow the " +
                                                      $"buffer of length {bufferLength} by " +
                                                      $"{(size + offset) - bufferLength} bytes");
        }
        
        /// <summary>
        /// Static check for doing bounds check on upper bound of a buffer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void CheckLowerBound(int bufferLength, int offset, int size)
        {
            if (offset >= 0) return;
            
            if (offset + size < 0)
                throw new ArgumentOutOfRangeException($"writing {size} bytes at offset {offset} would underoverflow the " +
                                                      $"buffer of length {bufferLength} by " +
                                                      $"{Math.Abs(offset + size)} bytes");
        }

        public abstract void Serialize(object value, byte[] buffer, int offset);
        
        public abstract object Deserialize(byte[] buffer, int offset); 
        
        public abstract int GetSizeFromBuffer(byte[] buffer, int offset);
        
        public abstract int GetSizeFromValue(object value);
    }
}