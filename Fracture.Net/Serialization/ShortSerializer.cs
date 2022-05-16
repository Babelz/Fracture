using System;
using Fracture.Common.Memory;

namespace Fracture.Net.Serialization
{
    /// <summary>
    /// Value serializer that provides serialization for <see cref="short"/>.
    /// </summary>
    [ValueSerializer(typeof(short))]
    public static class ShortSerializer
    {
        [ValueSerializer.SupportsType]
        public static bool SupportsType(Type type) => type == typeof(short);

        /// <summary>
        /// Writes given int16 value to given buffer beginning at given offset.
        /// </summary>
        [ValueSerializer.Serialize]
        public static void Serialize(short value, byte [] buffer, int offset) => MemoryMapper.WriteShort(value, buffer, offset);

        /// <summary>
        /// Reads next 2-bytes from given buffer beginning at given offset as int16
        /// and returns that value to the caller.
        /// </summary>
        [ValueSerializer.Deserialize]
        public static short Deserialize(byte [] buffer, int offset) => MemoryMapper.ReadShort(buffer, offset);

        /// <summary>
        /// Returns size of int16, should always be 2-bytes.
        /// </summary>
        [ValueSerializer.GetSizeFromBuffer]
        public static ushort GetSizeFromBuffer(byte [] buffer, int offset) => sizeof(short);

        /// <summary>
        /// Returns size of int16, should always be 2-bytes.
        /// </summary>
        [ValueSerializer.GetSizeFromValue]
        public static ushort GetSizeFromValue(short value) => sizeof(short);
    }

    /// <summary>
    /// Value serializer that provides serialization for <see cref="ushort"/>.
    /// </summary>
    [ValueSerializer(typeof(ushort))]
    public static class UshortSerializer
    {
        [ValueSerializer.SupportsType]
        public static bool SupportsType(Type type) => type == typeof(ushort);

        /// <summary>
        /// Writes given uint16 value to given buffer beginning at given offset.
        /// </summary>
        [ValueSerializer.Serialize]
        public static void Serialize(ushort value, byte [] buffer, int offset) => MemoryMapper.WriteUshort(value, buffer, offset);

        /// <summary>
        /// Reads next 2-bytes from given buffer beginning at given offset as uint16
        /// and returns that value to the caller.
        /// </summary>
        [ValueSerializer.Deserialize]
        public static ushort Deserialize(byte [] buffer, int offset) => MemoryMapper.ReadUshort(buffer, offset);

        /// <summary>
        /// Returns size of uint16, should always be 2-bytes.
        /// </summary>
        [ValueSerializer.GetSizeFromBuffer]
        public static ushort GetSizeFromBuffer(byte [] buffer, int offset) => sizeof(ushort);

        /// <summary>
        /// Returns size of uint16, should always be 2-bytes.
        /// </summary>
        [ValueSerializer.GetSizeFromValue]
        public static ushort GetSizeFromValue(ushort value) => sizeof(ushort);
    }
}