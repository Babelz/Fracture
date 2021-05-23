using System;
using Fracture.Common.Memory;

namespace Fracture.Net.Serialization
{
    /// <summary>
    /// Value serializer that provides serialization for <see cref="int"/>.
    /// </summary>
    public sealed class IntSerializer : ValueSerializer
    {
        public IntSerializer()
            : base(SerializationType.Int)
        {
        }
        
        public override bool SupportsType(Type type)
            => type == typeof(int);
        
        /// <summary>
        /// Writes given int32 value to given buffer beginning at given offset.
        /// </summary>
        public override void Serialize(object value, byte[] buffer, int offset)
        {
            base.Serialize(value, buffer, offset);
            
            MemoryMapper.WriteInt((int)value, buffer, offset);
        }
        
        /// <summary>
        /// Reads next 4-bytes from given buffer beginning at given offset as int32
        /// and returns that value to the caller.
        /// </summary>
        public override object Deserialize(byte[] buffer, int offset)
        {
            base.Deserialize(buffer, offset);
            
            return MemoryMapper.ReadInt(buffer, offset);
        }

        /// <summary>
        /// Returns size of int32, should always be 4-bytes.
        /// </summary>
        public override ushort GetSizeFromBuffer(byte[] buffer, int offset)
            => sizeof(int);
        
        /// <summary>
        /// Returns size of int32, should always be 4-bytes.
        /// </summary>
        public override ushort GetSizeFromValue(object value)
            => sizeof(int);
    }
    
    /// <summary>
    /// Value serializer that provides serialization for <see cref="uint"/>.
    /// </summary>
    public sealed class UintSerializer : ValueSerializer
    {
        public UintSerializer()
            : base(SerializationType.Uint)
        {
        }
        
        public override bool SupportsType(Type type)
            => type == typeof(uint);
        
        /// <summary>
        /// Writes given uint32 value to given buffer beginning at given offset.
        /// </summary>
        public override void Serialize(object value, byte[] buffer, int offset)
        {
            base.Serialize(value, buffer, offset);
            
            MemoryMapper.WriteUint((uint)value, buffer, offset);
        }
        
        /// <summary>
        /// Reads next 4-bytes from given buffer beginning at given offset as uint32
        /// and returns that value to the caller.
        /// </summary>
        public override object Deserialize(byte[] buffer, int offset)
        {
            base.Deserialize(buffer, offset);
            
            return MemoryMapper.ReadUint(buffer, offset);
        }

        /// <summary>
        /// Returns size of uint32, should always be 4-bytes.
        /// </summary>
        public override ushort GetSizeFromBuffer(byte[] buffer, int offset)
            => sizeof(uint);
        
        /// <summary>
        /// Returns size of uint32, should always be 4-bytes.
        /// </summary>
        public override ushort GetSizeFromValue(object value)
            => sizeof(uint);
    }
}