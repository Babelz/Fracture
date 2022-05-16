using System;
using System.Collections.Generic;
using Fracture.Common.Memory;

namespace Fracture.Net.Serialization
{
    /// <summary>
    /// Value serializer that provides serialization for enumeration types.
    /// </summary>
    [GenericValueSerializer]
    public static class EnumSerializer
    {
        #region Static fields
        private static readonly Dictionary<Type, SerializeDelegate<object>> SerializeDelegates = new Dictionary<Type, SerializeDelegate<object>>
        {
            { typeof(sbyte), (value, buffer, offset) => MemoryMapper.WriteSByte((sbyte)value, buffer, offset) },
            { typeof(byte), (value, buffer, offset) => MemoryMapper.WriteByte((byte)value, buffer, offset) },
            { typeof(short), (value, buffer, offset) => MemoryMapper.WriteShort((short)value, buffer, offset) },
            { typeof(ushort), (value, buffer, offset) => MemoryMapper.WriteUshort((ushort)value, buffer, offset) },
            { typeof(int), (value, buffer, offset) => MemoryMapper.WriteInt((int)value, buffer, offset) },
            { typeof(uint), (value, buffer, offset) => MemoryMapper.WriteUint((uint)value, buffer, offset) },
            { typeof(long), (value, buffer, offset) => MemoryMapper.WriteLong((long)value, buffer, offset) },
            { typeof(ulong), (value, buffer, offset) => MemoryMapper.WriteUlong((ulong)value, buffer, offset) }
        };

        private static readonly Dictionary<Type, DeserializeDelegate<object>> DeserializeDelegates = new Dictionary<Type, DeserializeDelegate<object>>
        {
            { typeof(sbyte), (buffer, offset) => MemoryMapper.ReadSByte(buffer, offset) },
            { typeof(byte), (buffer, offset) => MemoryMapper.ReadByte(buffer, offset) },
            { typeof(short), (buffer, offset) => MemoryMapper.ReadShort(buffer, offset) },
            { typeof(ushort), (buffer, offset) => MemoryMapper.ReadUshort(buffer, offset) },
            { typeof(int), (buffer, offset) => MemoryMapper.ReadInt(buffer, offset) },
            { typeof(uint), (buffer, offset) => MemoryMapper.ReadUint(buffer, offset) },
            { typeof(long), (buffer, offset) => MemoryMapper.ReadLong(buffer, offset) },
            { typeof(ulong), (buffer, offset) => MemoryMapper.ReadUlong(buffer, offset) }
        };

        private static readonly Dictionary<Type, ushort> SizeOfUnderlyingTypes = new Dictionary<Type, ushort>
        {
            { typeof(sbyte), sizeof(sbyte) },
            { typeof(byte), sizeof(byte) },
            { typeof(short), sizeof(short) },
            { typeof(ushort), sizeof(ushort) },
            { typeof(int), sizeof(int) },
            { typeof(uint), sizeof(uint) },
            { typeof(long), sizeof(long) },
            { typeof(ulong), sizeof(ulong) }
        };
        #endregion

        [ValueSerializer.SupportsType]
        public static bool SupportsType(Type type) => type.IsEnum;

        /// <summary>
        /// Writes given enumeration value to given buffer beginning at given offset.
        /// </summary>
        [ValueSerializer.Serialize]
        public static void Serialize<T>(T value, byte [] buffer, int offset) where T : struct, Enum =>
            SerializeDelegates[typeof(T).GetEnumUnderlyingType()](value, buffer, offset);

        /// <summary>
        /// Reads next n-bytes from given buffer beginning at given offset as enum type
        /// and returns that value to the caller.
        /// </summary>
        [ValueSerializer.Deserialize]
        public static T Deserialize<T>(byte [] buffer, int offset) where T : struct, Enum =>
            (T)DeserializeDelegates[typeof(T).GetEnumUnderlyingType()](buffer, offset);

        /// <summary>
        /// Returns size of enum, can vary between 1 to 4-bytes plus the added small content length size.
        /// </summary>
        [ValueSerializer.GetSizeFromBuffer]
        public static ushort GetSizeFromBuffer<T>(byte [] buffer, int offset) where T : struct, Enum =>
            SizeOfUnderlyingTypes[typeof(T).GetEnumUnderlyingType()];

        /// <summary>
        /// Returns size of enumeration value, size can vary between 1 to 4-bytes plus the added small content length size.
        /// </summary>
        [ValueSerializer.GetSizeFromValue]
        public static ushort GetSizeFromValue<T>(T value) where T : struct, Enum => SizeOfUnderlyingTypes[typeof(T).GetEnumUnderlyingType()];
    }
}