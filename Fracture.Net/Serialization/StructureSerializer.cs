using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Fracture.Net.Serialization.Generation.Builders;

namespace Fracture.Net.Serialization
{
    /// <summary>
    /// Generic value serializer that provides serialization for structure types. Structure types are any user defined value types or structures that can be
    /// mapped for serialization. The serializer can be used for serialization and deserialization in two ways:
    ///     - Type information is passed via generic interface when the type is known at runtime
    ///     - Type information is not passed and object value will be used instead
    ///
    /// The serializer stores runtime type information about the serialized types to binary format for keeping the type information between serialization calls.
    /// Because the actual serialization delegates are generated dynamically at runtime, values are always boxed when they are serialized or deserialized.
    /// </summary>
    [GenericValueSerializer]
    public static class StructSerializer
    {
        #region Static fields
        private static readonly Dictionary<Type, ushort> SerializationTypeIdMappings = new Dictionary<Type, ushort>();
        private static readonly Dictionary<ushort, Type> RunTypeMappings             = new Dictionary<ushort, Type>();

        private static readonly Dictionary<Type, DynamicSerializeDelegate> SerializeDelegates         
            = new Dictionary<Type, DynamicSerializeDelegate>();
        
        private static readonly Dictionary<Type, DynamicDeserializeDelegate> DeserializeDelegates       
            = new Dictionary<Type, DynamicDeserializeDelegate>();
        
        private static readonly Dictionary<Type, DynamicGetSizeFromValueDelegate> GetSizeFromValueDelegates 
            = new Dictionary<Type, DynamicGetSizeFromValueDelegate>();
        
        private static long NextSerializationTypeId;
        #endregion
        
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ushort InternalGetSizeFromValue(Type serializationType, object value)
            => checked((ushort)(GetSizeFromValueDelegates[serializationType](value) + Protocol.ContentLength.Size + Protocol.SerializationTypeId.Size));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object InternalDeserialize(byte[] buffer, int offset)
        {
            offset += Protocol.ContentLength.Size;
            
            var serializationTypeId = Protocol.SerializationTypeId.Read(buffer, offset);
            offset += Protocol.SerializationTypeId.Size;
            
            var runType = RunTypeMappings[serializationTypeId];
            
            return DeserializeDelegates[runType](buffer, offset);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void InternalSerialize(Type serializationType, object value, byte[] buffer, int offset)
        {
            var serializationTypeId = SerializationTypeIdMappings[serializationType];
            
            var contentLength = checked((ushort)(GetSizeFromValueDelegates[serializationType](value) + 
                                                 Protocol.ContentLength.Size + 
                                                 Protocol.SerializationTypeId.Size));
            
            Protocol.ContentLength.Write(contentLength, buffer, offset);
            offset += Protocol.ContentLength.Size;
            
            Protocol.SerializationTypeId.Write(serializationTypeId, buffer, offset);
            offset += Protocol.SerializationTypeId.Size;
                
            SerializeDelegates[serializationType](value, buffer, offset);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RegisterStructureTypeSerializer(Type serializationType,
                                                           DynamicSerializeDelegate serializeDelegate,
                                                           DynamicDeserializeDelegate deserializeDelegate,
                                                           DynamicGetSizeFromValueDelegate getSizeFromValueDelegate)
        {
            if (SupportsType(serializationType))
                throw new InvalidOperationException("serialization type already registered")
                {
                    Data = { { nameof(serializationType), serializationType } }
                };
            
            SerializeDelegates.Add(serializationType, serializeDelegate);
            DeserializeDelegates.Add(serializationType, deserializeDelegate);
            GetSizeFromValueDelegates.Add(serializationType, getSizeFromValueDelegate);
            
            var serializationTypeId = checked((ushort)(Interlocked.Read(ref NextSerializationTypeId)));
            
            SerializationTypeIdMappings.Add(serializationType, serializationTypeId);
            RunTypeMappings.Add(serializationTypeId, serializationType);

            Interlocked.Increment(ref NextSerializationTypeId);
        }

        [ValueSerializer.SupportsType]
        public static bool SupportsType(Type type)
            => SerializeDelegates.ContainsKey(type);
        
        /// <summary>
        /// Writes given structure to given buffer beginning at given offset.
        /// </summary>
        [ValueSerializer.Serialize]
        public static void Serialize<T>(T value, byte[] buffer, int offset)
            => InternalSerialize(typeof(T), value, buffer, offset);

        /// <summary>
        /// Writes given structure to given buffer beginning at given offset.
        /// </summary>
        public static void Serialize(object value, byte[] buffer, int offset)
            => InternalSerialize(value.GetType(), value, buffer, offset);
        
        /// <summary>
        /// Reads next n-bytes from given buffer beginning at given offset as structure and returns that value to the caller. Type information is retrieved the
        /// buffer for the object.
        /// </summary>
        [ValueSerializer.Deserialize]
        public static T Deserialize<T>(byte[] buffer, int offset)
            => (T)InternalDeserialize(buffer, offset);
        
        /// <summary>
        /// Reads next n-bytes from given buffer beginning at given offset as structure and returns that value to the caller. Type information is retrieved the
        /// buffer for the object.
        /// </summary>
        public static object Deserialize(byte[] buffer, int offset)
            => InternalDeserialize(buffer, offset);
        
        /// <summary>
        /// Returns size of structure, size will vary.
        /// </summary>
        [ValueSerializer.GetSizeFromBuffer]
        public static ushort GetSizeFromBuffer(byte[] buffer, int offset)
            => Protocol.ContentLength.Read(buffer, offset);
                
        /// <summary>
        /// Returns size of structure value, size will vary. Type information is retrieved from the proved generic type argument.
        /// </summary>
        [ValueSerializer.GetSizeFromValue]
        public static ushort GetSizeFromValue<T>(T value)
            => InternalGetSizeFromValue(typeof(T), value);
        
        /// <summary>
        /// Returns size of structure value, size will vary. Type information is retrieved from the provided object.
        /// </summary>
        public static ushort GetSizeFromValue(object value)
            => InternalGetSizeFromValue(value.GetType(), value);
    }
}