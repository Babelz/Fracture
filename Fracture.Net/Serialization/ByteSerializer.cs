using System;
using Fracture.Common.Memory;

namespace Fracture.Net.Serialization
{
    /// <summary>
    /// Value serializer that provides serialization for <see cref="byte"/>.
    /// </summary>
    [ValueSerializer(typeof(byte))]
    public static class ByteSerializer
    {
        [ValueSerializer.SupportsType]
        public static bool SupportsType(Type type) => type == typeof(byte);

        /// <summary>
        /// Writes given byte value to given buffer beginning at given offset.
        /// </summary>
        [ValueSerializer.Serialize]
        public static void Serialize(byte value, byte [] buffer, int offset)
        {
            MemoryMapper.WriteByte(value, buffer, offset);
        }

        /// <summary>
        /// Reads next 1-bytes from given buffer beginning at given offset as byte
        /// and returns that value to the caller.
        /// </summary>
        [ValueSerializer.Deserialize]
        public static byte Deserialize(byte [] buffer, int offset)
        {
            return MemoryMapper.ReadByte(buffer, offset);
        }

        /// <summary>
        /// Returns size of byte, should always be 1-bytes.
        /// </summary>
        [ValueSerializer.GetSizeFromBuffer]
        public static ushort GetSizeFromBuffer(byte [] buffer, int offset) => sizeof(byte);

        /// <summary>
        /// Returns size of byte, should always be 1-bytes.
        /// </summary>
        [ValueSerializer.GetSizeFromValue]
        public static ushort GetSizeFromValue(byte value) => sizeof(byte);
    }

    /// <summary>
    /// Value serializer that provides serialization for <see cref="sbyte"/>.
    /// </summary>
    [ValueSerializer(typeof(sbyte))]
    public static class SbyteSerializer
    {
        [ValueSerializer.SupportsType]
        public static bool SupportsType(Type type) => type == typeof(sbyte);

        /// <summary>
        /// Writes given sbyte value to given buffer beginning at given offset.
        /// </summary>
        [ValueSerializer.Serialize]
        public static void Serialize(sbyte value, byte [] buffer, int offset) => MemoryMapper.WriteSByte(value, buffer, offset);

        /// <summary>
        /// Reads next 1-bytes from given buffer beginning at given offset as sbyte
        /// and returns that value to the caller.
        /// </summary>
        [ValueSerializer.Deserialize]
        public static sbyte Deserialize(byte [] buffer, int offset) => MemoryMapper.ReadSByte(buffer, offset);

        /// <summary>
        /// Returns size of sbyte, should always be 1-bytes.
        /// </summary>
        [ValueSerializer.GetSizeFromBuffer]
        public static ushort GetSizeFromBuffer(byte [] buffer, int offset) => sizeof(sbyte);

        /// <summary>
        /// Returns size of sbyte, should always be 1-bytes.
        /// </summary>
        [ValueSerializer.GetSizeFromValue]
        public static ushort GetSizeFromValue(sbyte value) => sizeof(sbyte);
    }

    /// <summary>
    /// Value serializer that provides serialization <see cref="bool"/>.
    /// </summary>
    [ValueSerializer(typeof(bool))]
    public static class BoolSerializer
    {
        [ValueSerializer.SupportsType]
        public static bool SupportsType(Type type) => type == typeof(bool);

        /// <summary>
        /// Writes given bool value to given buffer beginning at given offset.
        /// </summary>
        [ValueSerializer.Serialize]
        public static void Serialize(bool value, byte [] buffer, int offset) => MemoryMapper.WriteBool(value, buffer, offset);

        /// <summary>
        /// Reads next 1-bytes from given buffer beginning at given offset as bool
        /// and returns that value to the caller.
        /// </summary>
        [ValueSerializer.Deserialize]
        public static bool Deserialize(byte [] buffer, int offset) => MemoryMapper.ReadBool(buffer, offset);

        /// <summary>
        /// Returns size of bool, should always be 1-bytes.
        /// </summary>
        [ValueSerializer.GetSizeFromBuffer]
        public static ushort GetSizeFromBuffer(byte [] buffer, int offset) => sizeof(bool);

        /// <summary>
        /// Returns size of bool, should always be 1-bytes.
        /// </summary>
        [ValueSerializer.GetSizeFromValue]
        public static ushort GetSizeFromValue(bool value) => sizeof(bool);
    }
}