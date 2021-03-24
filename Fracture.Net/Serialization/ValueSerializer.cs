using System;
using System.Runtime.CompilerServices;
using System.Xml.Schema;

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
        /// Gets the size of the value from given buffer at given offset. This size is the values size
        /// inside the buffer when it is serialized.
        /// </summary>
        ushort GetSizeFromBuffer(byte[] buffer, int offset);
        
        /// <summary>
        /// Returns the size of the value when it is serialized.
        /// </summary>
        ushort GetSizeFromValue(object value);
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
        }
        
        /// <summary>
        /// Gets the runtime type id that this value serializer writes and accepts from the serialization buffer. This
        /// value should be static to the class.
        /// </summary>
        public static ushort SerializationType
        {
            get;
        } 
        #endregion
        
        static ValueSerializer()
        {
          SerializationType = SerializationTypeGenerator.Next();
          RuntimeType       = typeof(T);
        }
        
        protected ValueSerializer()
        { 
        }
        
        /// <summary>
        /// Checks both upper and lower bounds of a buffer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void CheckBufferBounds(int bufferLength, int offset, int size)
        {
            if (offset >= 0)
            {
                // Check upper bound.
                if (offset + size >= bufferLength)
                    throw new ArgumentOutOfRangeException($"writing {size} bytes at offset {offset} would overflow the " +
                                                          $"buffer of length {bufferLength} by " +
                                                          $"{(size + offset) - bufferLength} bytes");
            }
            else
            {
                // Check lower bound.
                if (offset + size < 0)
                    throw new ArgumentOutOfRangeException($"writing {size} bytes at offset {offset} would underflow the " +
                                                          $"buffer of length {bufferLength} by " +
                                                          $"{Math.Abs(offset + size)} bytes");   
            }
        }

        public virtual void Serialize(object value, byte[] buffer, int offset)
        {
            var size = GetSizeFromValue(value);
            
            CheckBufferBounds(buffer.Length, offset, size);
        }
        
        public virtual object Deserialize(byte[] buffer, int offset)
        {
            var size = GetSizeFromBuffer(buffer, offset);
            
            CheckBufferBounds(buffer.Length, offset, size);
            
            return null;
        }
        
        public abstract ushort GetSizeFromBuffer(byte[] buffer, int offset);
        
        public abstract ushort GetSizeFromValue(object value);
    }
}