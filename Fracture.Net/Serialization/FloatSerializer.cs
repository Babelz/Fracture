using System;
using Fracture.Common.Memory;

namespace Fracture.Net.Serialization
{
    /// <summary>
    /// Value serializer that provides serialization for <see cref="float"/>.
    /// </summary>
    [ValueSerializer(typeof(float))]
    public static class FloatSerializer
    {
        [ValueSerializer.SupportsType]
        public static bool SupportsType(Type type) => type == typeof(float);

        /// <summary>
        /// Writes given float value to given buffer beginning at given offset.
        /// </summary>
        [ValueSerializer.Serialize]
        public static void Serialize(float value, byte [] buffer, int offset) => MemoryMapper.WriteFloat(value, buffer, offset);

        /// <summary>
        /// Reads next 4-bytes from given buffer beginning at given offset as float
        /// and returns that value to the caller.
        /// </summary>
        [ValueSerializer.Deserialize]
        public static float Deserialize(byte [] buffer, int offset) => MemoryMapper.ReadFloat(buffer, offset);

        /// <summary>
        /// Returns size of float, should always be 4-bytes.
        /// </summary>
        [ValueSerializer.GetSizeFromBuffer]
        public static ushort GetSizeFromBuffer(byte [] buffer, int offset) => sizeof(float);

        /// <summary>
        /// Returns size of float, should always be 4-bytes.
        /// </summary>
        [ValueSerializer.GetSizeFromValue]
        public static ushort GetSizeFromValue(float value) => sizeof(float);
    }

    /// <summary>
    /// Value serializer that provides serialization for <see cref="double"/>.
    /// </summary>
    [ValueSerializer(typeof(double))]
    public static class DoubleSerializer
    {
        [ValueSerializer.SupportsType]
        public static bool SupportsType(Type type) => type == typeof(double);

        /// <summary>
        /// Writes given double value to given buffer beginning at given offset.
        /// </summary>
        [ValueSerializer.Serialize]
        public static void Serialize(double value, byte [] buffer, int offset) => MemoryMapper.WriteDouble(value, buffer, offset);

        /// <summary>
        /// Reads next 8-bytes from given buffer beginning at given offset as double
        /// and returns that value to the caller.
        /// </summary>
        [ValueSerializer.Deserialize]
        public static double Deserialize(byte [] buffer, int offset) => MemoryMapper.ReadDouble(buffer, offset);

        /// <summary>
        /// Returns size of double, should always be 8-bytes.
        /// </summary>
        [ValueSerializer.GetSizeFromBuffer]
        public static ushort GetSizeFromBuffer(byte [] buffer, int offset) => sizeof(double);

        /// <summary>
        /// Returns size of double, should always be 8-bytes.
        /// </summary>
        [ValueSerializer.GetSizeFromValue]
        public static ushort GetSizeFromValue(double value) => sizeof(double);
    }
}