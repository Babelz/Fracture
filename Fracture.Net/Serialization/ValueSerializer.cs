using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using NLog;

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
    /// Static registry class that holds all value serializers found from loaded assemblies. 
    /// </summary>
    public static class ValueSerializerRegistry
    {
        #region Static fields
        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        
        private static readonly Dictionary<byte, ValueSerializer> serializationTypeMap;
        
        private static readonly List<ValueSerializer> serializers;
        #endregion
        
        static ValueSerializerRegistry()
        {
            serializers          = GetSerializerTypes().Select(t => (ValueSerializer)Activator.CreateInstance(t)).ToList();
            serializationTypeMap = serializers.ToDictionary((v) => v.SerializedType, (v) => v);
        }
        
        /// <summary>
        /// Gets all types that are assignable from <see cref="ValueSerializer"/>, are classes and are not abstract.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IEnumerable<Type> GetSerializerTypes()
        {
            var types = new List<Type>();
            
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().OrderBy(a => a.FullName))
            {
                try
                {
                    types.AddRange(assembly.GetTypes()
                                           .Where(t => !t.IsAbstract && t.IsClass && typeof(ValueSerializer).IsAssignableFrom(t))
                                           .OrderBy(t => t.Name));
                }   
                catch (ReflectionTypeLoadException e)
                {
                    log.Warn(e, $"{nameof(ReflectionTypeLoadException)} occured while loading assemblies");
                }
            }
            
            return types;
        }    
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueSerializer GetValueSerializerForRunType(Type type)
            => serializers.First(v => v.SupportsType(type));
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueSerializer GetValueSerializerForSerializationType(byte serializationType)
            => serializationTypeMap[serializationType];
    }
}