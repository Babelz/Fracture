using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Threading;
using Fracture.Net.Serialization.Generation.Builders;

namespace Fracture.Net.Serialization
{
    [ValueSerializer(typeof(object))]
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
        
        private static long NextSerializationTypeId = 0;
        #endregion
        
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
        public static void Serialize(object value, byte[] buffer, int offset)
        {
            var type                = value.GetType();
            var serializationTypeId = SerializationTypeIdMappings[type];
            var contentLength       = checked((ushort)(GetSizeFromValueDelegates[type](value) + Protocol.ContentLength.Size + Protocol.SerializationTypeId.Size));
            
            Protocol.ContentLength.Write(contentLength, buffer, offset);
            offset += Protocol.ContentLength.Size;
            
            Protocol.SerializationTypeId.Write(serializationTypeId, buffer, offset);
            offset += Protocol.SerializationTypeId.Size;
                
            SerializeDelegates[type](value, buffer, offset);
        }
            
        /// <summary>
        /// Reads next n-bytes from given buffer beginning at given offset as structure
        /// and returns that value to the caller.
        /// </summary>
        [ValueSerializer.Deserialize]
        public static object Deserialize(byte[] buffer, int offset)
        {
            offset += Protocol.ContentLength.Size;
            
            var serializationTypeId = Protocol.SerializationTypeId.Read(buffer, offset);
            offset += Protocol.SerializationTypeId.Size;
            
            var runType = RunTypeMappings[serializationTypeId];
            
            return DeserializeDelegates[runType](buffer, offset);
        }
        
        /// <summary>
        /// Returns size of structure, size will vary.
        /// </summary>
        [ValueSerializer.GetSizeFromBuffer]
        public static ushort GetSizeFromBuffer(byte[] buffer, int offset)
            => Protocol.ContentLength.Read(buffer, offset);
                
        /// <summary>
        /// Returns size of structure value, size will vary.
        /// </summary>
        [ValueSerializer.GetSizeFromValue]
        public static ushort GetSizeFromValue(object value)
            => checked((ushort)(GetSizeFromValueDelegates[value.GetType()](value) + Protocol.ContentLength.Size + Protocol.SerializationTypeId.Size));
    }
}