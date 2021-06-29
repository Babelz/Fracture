using System;
using Fracture.Common.Memory;

namespace Fracture.Net.Serialization
{
    /// <summary>
    /// Value serializer that provides serialization for <see cref="int"/>.
    /// </summary>
    [ValueSerializer(typeof(int))]
    public static class IntSerializer
    {
        [ValueSerializer.SupportsType]
        public static bool SupportsType(Type type)
            => type == typeof(int);
        
        /// <summary>
        /// Writes given int32 value to given buffer beginning at given offset.
        /// </summary>
        [ValueSerializer.Serialize]
        public static void Serialize(int value, byte[] buffer, int offset)
        {
            MemoryMapper.WriteInt(value, buffer, offset);
        }
        
        /// <summary>
        /// Reads next 4-bytes from given buffer beginning at given offset as int32
        /// and returns that value to the caller.
        /// </summary>
        [ValueSerializer.Deserialize]
        public static int Deserialize(byte[] buffer, int offset)
        {
            return MemoryMapper.ReadInt(buffer, offset);
        }

        /// <summary>
        /// Returns size of int32, should always be 4-bytes.
        /// </summary>
        [ValueSerializer.GetSizeFromBuffer]
        public static ushort GetSizeFromBuffer(byte[] buffer, int offset)
            => sizeof(int);
        
        /// <summary>
        /// Returns size of int32, should always be 4-bytes.
        /// </summary>
        [ValueSerializer.GetSizeFromValue]
        public static ushort GetSizeFromValue(int value)
            => sizeof(int);
    }
    
    /// <summary>
    /// Value serializer that provides serialization for <see cref="uint"/>.
    /// </summary>
    [ValueSerializer(typeof(uint))]
    public static class UintSerializer 
    {
        [ValueSerializer.SupportsType]
        public static bool SupportsType(Type type)
            => type == typeof(uint);
        
        /// <summary>
        /// Writes given uint32 value to given buffer beginning at given offset.
        /// </summary>
        [ValueSerializer.Serialize]
        public static void Serialize(uint value, byte[] buffer, int offset)
        {
            MemoryMapper.WriteUint(value, buffer, offset);
        }
        
        /// <summary>
        /// Reads next 4-bytes from given buffer beginning at given offset as uint32
        /// and returns that value to the caller.
        /// </summary>
        [ValueSerializer.Deserialize]
        public static uint Deserialize(byte[] buffer, int offset)
        {
            return MemoryMapper.ReadUint(buffer, offset);
        }

        /// <summary>
        /// Returns size of uint32, should always be 4-bytes.
        /// </summary>
        [ValueSerializer.GetSizeFromBuffer]
        public static ushort GetSizeFromBuffer(byte[] buffer, int offset)
            => sizeof(uint);
        
        /// <summary>
        /// Returns size of uint32, should always be 4-bytes.
        /// </summary>
        [ValueSerializer.GetSizeFromValue]
        public static ushort GetSizeFromValue(uint value)
            => sizeof(uint);
    }
}