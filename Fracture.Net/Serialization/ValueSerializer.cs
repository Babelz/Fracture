using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Fracture.Net.Serialization
{
    /// <summary>
    /// Enumeration containing all supported build in types for serialization.
    /// </summary>
    public enum SerializationType : byte
    {
        Null = 0,
        Sbyte,
        Short,
        Int,
        Long,
        Byte,
        Ushort,
        Uint,
        Ulong,
        Float,
        Double,
        String,
        Char,
        Bool,
        DateTime,
        TimeSpan,
        Enum,
        Structure,
        Array,
        List,
        Dictionary
    }

    /// <summary>
    /// Abstract base class for creating type free serializers for serializing single values. These values
    /// can be anything from single primitive to more complex classes and specific types like lists. 
    /// </summary>
    public abstract class ValueSerializer
    {
        #region Properties
        /// <summary>
        /// Gets the runtime type id that this value serializer writes and accepts from the serialization buffer. 
        /// </summary>
        public byte SerializedType
        {
            get;
        }
        #endregion
        
        /// <summary>
        /// Constructor to allow extension of build in serialization types.
        /// </summary>
        protected ValueSerializer(byte serializedType)
        {
            SerializedType = serializedType;
        }
        
        /// <summary>
        /// Constructor to use build in serialization types.
        /// </summary>
        protected ValueSerializer(SerializationType serializationType)
            : this((byte)serializationType)
        {
        }

        /// <summary>
        /// Checks both upper and lower bounds of a buffer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckBufferBounds(int bufferLength, int offset, int size)
        {
            if (offset >= 0)
            {
                // Check upper bound.
                if (offset + size > bufferLength)
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
        
        /// <summary>
        /// Returns boolean declaring if this serializer supports given type.
        /// </summary>
        public abstract bool SupportsType(Type type);

        /// <summary>
        /// Serializes given value to given buffer starting at given offset.
        /// </summary>
        public virtual void Serialize(object value, byte[] buffer, int offset)
        {
            // Check current offset bounds with zero content size, offset can under or overflow the
            // bounds of the buffer.
            CheckBufferBounds(buffer.Length, offset, 0);
            
            var size = GetSizeFromValue(value);
            
            // Same here, check bounds for the actual value size.
            CheckBufferBounds(buffer.Length, offset, size);
        }
        
        /// <summary>
        /// Deserializes value from given buffer starting at given offset.
        /// </summary>
        public virtual object Deserialize(byte[] buffer, int offset)
        {   
            // Check current offset bounds with zero content size, offset can under or overflow the
            // bounds of the buffer.
            CheckBufferBounds(buffer.Length, offset, 0);

            var size = GetSizeFromBuffer(buffer, offset);
            
            // Check bounds for the actual value size.
            CheckBufferBounds(buffer.Length, offset, size);
            
            return null;
        }
        
        /// <summary>
        /// Gets the size of the value from given buffer at given offset. This size is the values size
        /// inside the buffer when it is serialized.
        /// </summary>
        public abstract ushort GetSizeFromBuffer(byte[] buffer, int offset);
        
        /// <summary>
        /// Returns the size of the value when it is serialized.
        /// </summary>
        public abstract ushort GetSizeFromValue(object value);
    }

    /// <summary>
    /// Static utility class for creating value serializers. Expect all methods in this class to be slow as they all
    /// rely heavily on reflection for locating the serialization classes from all of the loaded assemblies in current
    /// app domain.
    /// </summary>
    public static class ValueSerializerFactory
    {
        /// <summary>
        /// Gets all types that are assignable from <see cref="ValueSerializer"/>, are classes and are not abstract.
        /// Orders all types by their name as the order of instantiation effects the order of the serialization types
        /// of each serializer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IEnumerable<Type> GetSerializerTypes()
            => AppDomain.CurrentDomain.GetAssemblies()
                                      .SelectMany(a => a.GetTypes())
                                      .Where(t => !t.IsAbstract && 
                                                  t.IsClass && 
                                                  t.IsAssignableFrom(typeof(ValueSerializer)))
                                      .OrderBy(t => t.Name);

        /// <summary>
        /// Creates instance of all known serializers and returns them to the caller.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<ValueSerializer> GetSerializers() 
            => GetSerializerTypes().Select(t => (ValueSerializer)Activator.CreateInstance(t));
        
        /// <summary>
        /// Creates new map that maps serialization types to their serializers. 
        /// </summary>
        public static IDictionary<byte, ValueSerializer> GetSerializationTypeMap()
            => GetSerializerTypes().Select(t => (ValueSerializer)Activator.CreateInstance(t))
                                   .ToDictionary(
                                       (v) => v.SerializedType, 
                                       (v) => v);
    }
}