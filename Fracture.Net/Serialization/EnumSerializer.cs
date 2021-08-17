using System;
using System.Collections.Generic;

namespace Fracture.Net.Serialization
{
    /// <summary>
    /// Value serializer that provides serialization for enumeration types.
    /// </summary>
    [GenericValueSerializer]
    public static class EnumSerializer 
    {
        #region Static fields
        private static readonly ValueSerializerTypeRegistry Registry = new ValueSerializerTypeRegistry();
        
        private static readonly List<Delegate> SerializeDelegates         = new List<Delegate>();
        private static readonly List<Delegate> DeserializeDelegates       = new List<Delegate>();
        private static readonly List<Delegate> GetSizeFromValueDelegates  = new List<Delegate>();
        private static readonly List<Delegate> GetSizeFromBufferDelegates = new List<Delegate>();
        #endregion
        
        [ValueSerializer.SupportsType]
        public static bool SupportsType(Type type)
            => Registry.IsSpecializedRunType(type) && type.IsEnum;
        
        /// <summary>
        /// Writes given enumeration value to given buffer beginning at given offset.
        /// </summary>
        [ValueSerializer.Serialize]
        public static void Serialize<T>(T value, byte[] buffer, int offset) where T : Enum
            => ((SerializeDelegate<T>)SerializeDelegates[Registry.GetSerializationTypeId(value.GetType())])(value, buffer, offset + Protocol.SerializationTypeId.Size);
        
        /// <summary>
        /// Reads next n-bytes from given buffer beginning at given offset as enum type
        /// and returns that value to the caller.
        /// </summary>
        [ValueSerializer.Deserialize]
        public static T Deserialize<T>(byte[] buffer, int offset) where T : Enum
            => ((DeserializeDelegate<T>)DeserializeDelegates[Protocol.SerializationTypeId.Read(buffer, offset)])(buffer, offset + Protocol.SerializationTypeId.Size);

        /// <summary>
        /// Returns size of uint32, should always be 4-bytes.
        /// </summary>
        [ValueSerializer.GetSizeFromBuffer]
        public static ushort GetSizeFromBuffer(byte[] buffer, int offset)
            => ((GetSizeFromBufferDelegate)GetSizeFromBufferDelegates[Protocol.SerializationTypeId.Read(buffer, offset)])(buffer, offset + Protocol.SerializationTypeId.Size);
        
        /// <summary>
        /// Returns size of enumeration value, size can vary.
        /// </summary>
        [ValueSerializer.GetSizeFromValue]
        public static ushort GetSizeFromValue<T>(T value) where T : Enum
            => ((GetSizeFromValueDelegate<T>)GetSizeFromValueDelegates[Registry.GetSerializationTypeId(value.GetType())])(value);
        
        public static void Register(Type type, 
                                    Delegate serializeDelegate, 
                                    Delegate deserializeDelegate, 
                                    Delegate getSizeFromValueDelegate,
                                    Delegate getSizeFromBufferDelegate)
        {
            if (!type.IsEnum)
                throw new ValueSerializerSchemaException($"expecting enum type but instead got {type.Name}", typeof(EnumSerializer));
            
            var serializationTypeId = Registry.Specialize(type);
            
            SerializeDelegates.Insert(serializationTypeId, serializeDelegate);
            DeserializeDelegates.Insert(serializationTypeId, deserializeDelegate);
            GetSizeFromValueDelegates.Insert(serializationTypeId, getSizeFromValueDelegate);
            GetSizeFromBufferDelegates.Insert(serializationTypeId, getSizeFromBufferDelegate);
        }
    }
}