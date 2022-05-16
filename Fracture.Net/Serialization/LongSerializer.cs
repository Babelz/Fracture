using System;
using Fracture.Common.Memory;

namespace Fracture.Net.Serialization
{
    /// <summary>
    /// Value serializer that provides serialization for <see cref="long"/>.
    /// </summary>
    [ValueSerializer(typeof(long))]
    public static class LongSerializer
    {
        [ValueSerializer.SupportsType]
        public static bool SupportsType(Type type) => type == typeof(long);

        /// <summary>
        /// Writes given int64 value to given buffer beginning at given offset.
        /// </summary>
        [ValueSerializer.Serialize]
        public static void Serialize(long value, byte [] buffer, int offset) => MemoryMapper.WriteLong(value, buffer, offset);

        /// <summary>
        /// Reads next 8-bytes from given buffer beginning at given offset as int32
        /// and returns that value to the caller.
        /// </summary>
        [ValueSerializer.Deserialize]
        public static long Deserialize(byte [] buffer, int offset) => MemoryMapper.ReadLong(buffer, offset);

        /// <summary>
        /// Returns size of int64, should always be 8-bytes.
        /// </summary>
        [ValueSerializer.GetSizeFromBuffer]
        public static ushort GetSizeFromBuffer(byte [] buffer, int offset) => sizeof(long);

        /// <summary>
        /// Returns size of int64, should always be 8-bytes.
        /// </summary>
        [ValueSerializer.GetSizeFromValue]
        public static ushort GetSizeFromValue(long value) => sizeof(long);
    }

    /// <summary>
    /// Value serializer that provides serialization for <see cref="ulong"/>.
    /// </summary>
    [ValueSerializer(typeof(ulong))]
    public static class UlongSerializer
    {
        [ValueSerializer.SupportsType]
        public static bool SupportsType(Type type) => type == typeof(ulong);

        /// <summary>
        /// Writes given uint64 value to given buffer beginning at given offset.
        /// </summary>
        [ValueSerializer.Serialize]
        public static void Serialize(ulong value, byte [] buffer, int offset) => MemoryMapper.WriteUlong(value, buffer, offset);

        /// <summary>
        /// Reads next 8-bytes from given buffer beginning at given offset as uint32
        /// and returns that value to the caller.
        /// </summary>
        [ValueSerializer.Deserialize]
        public static ulong Deserialize(byte [] buffer, int offset) => MemoryMapper.ReadUlong(buffer, offset);

        /// <summary>
        /// Returns size of uint64, should always be 8-bytes.
        /// </summary>
        [ValueSerializer.GetSizeFromBuffer]
        public static ushort GetSizeFromBuffer(byte [] buffer, int offset) => sizeof(ulong);

        /// <summary>
        /// Returns size of uint64, should always be 8-bytes.
        /// </summary>
        [ValueSerializer.GetSizeFromValue]
        public static ushort GetSizeFromValue(ulong value) => sizeof(ulong);
    }
}