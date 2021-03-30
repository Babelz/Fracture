using System;
using System.Collections.Generic;
using System.Linq;
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

        public virtual void Serialize(object value, byte[] buffer, int offset)
        {
            // Check current offset bounds with zero content size, offset can under or overflow the
            // bounds of the buffer.
            CheckBufferBounds(buffer.Length, offset, 0);
            
            var size = GetSizeFromValue(value);
            
            // Same here, check bounds for the actual value size.
            CheckBufferBounds(buffer.Length, offset, size);
        }
        
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
        
        public abstract ushort GetSizeFromBuffer(byte[] buffer, int offset);
        
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
        /// Gets all types that are assignable from <see cref="IValueSerializer"/>, are classes and are not abstract.
        /// Orders all types by their name as the order of instantiation effects how the serialization type of each
        /// serializer will be.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IEnumerable<Type> GetSerializerTypes()
            => AppDomain.CurrentDomain.GetAssemblies()
                                      .SelectMany(a => a.GetTypes())
                                      .Where(t => !t.IsAbstract && 
                                                  t.IsClass && 
                                                  t.IsAssignableFrom(typeof(IValueSerializer)))
                                      .OrderBy(t => t.Name);

        /// <summary>
        /// Creates new map that maps serialization types to their serializers. 
        /// </summary>
        public static IDictionary<ushort, IValueSerializer> CreateSerializationTypeMap()
            => GetSerializerTypes().Select(t => (IValueSerializer)Activator.CreateInstance(t))
                                   .ToDictionary(
                                       (v) => (ushort)v.GetType().GetProperty("SerializationType")!.GetValue(null), 
                                       (v) => v);
        
        /// <summary>
        /// Creates new map that maps runtime types to their serializers.
        /// </summary>
        public static IDictionary<Type, IValueSerializer> CreateRuntimeTypeMap()
            => GetSerializerTypes().Select(t => (IValueSerializer)Activator.CreateInstance(t))
                                   .ToDictionary(
                                       (v) => (Type)v.GetType().GetProperty("RuntimeType")!.GetValue(null), 
                                       (v) => v);
    }
}