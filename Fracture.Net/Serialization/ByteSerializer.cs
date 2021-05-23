using System;
using Fracture.Common.Memory;

namespace Fracture.Net.Serialization
{
    /// <summary>
    /// Value serializer that provides serialization for <see cref="byte"/>.
    /// </summary>
    public sealed class ByteSerializer : ValueSerializer
    {
        public ByteSerializer()
            : base(SerializationType.Byte)
        {
        }
        
        public override bool SupportsType(Type type)
            => type == typeof(byte);

        /// <summary>
        /// Writes given byte value to given buffer beginning at given offset.
        /// </summary>
        public override void Serialize(object value, byte[] buffer, int offset)
        {
            base.Serialize(value, buffer, offset);
            
            MemoryMapper.WriteByte((byte)value, buffer, offset);
        }
        
        /// <summary>
        /// Reads next 1-bytes from given buffer beginning at given offset as byte
        /// and returns that value to the caller.
        /// </summary>
        public override object Deserialize(byte[] buffer, int offset)
        {
            base.Deserialize(buffer, offset);
            
            return MemoryMapper.ReadByte(buffer, offset);
        }

        /// <summary>
        /// Returns size of byte, should always be 1-bytes.
        /// </summary>
        public override ushort GetSizeFromBuffer(byte[] buffer, int offset)
            => sizeof(byte);
        
        /// <summary>
        /// Returns size of byte, should always be 1-bytes.
        /// </summary>
        public override ushort GetSizeFromValue(object value)
            => sizeof(byte);
    }
    
    /// <summary>
    /// Value serializer that provides serialization for <see cref="sbyte"/>.
    /// </summary>
    public sealed class SbyteSerializer : ValueSerializer
    {
        public SbyteSerializer()
            : base(SerializationType.Sbyte)
        {
        }
        
        public override bool SupportsType(Type type)
            => type == typeof(sbyte);

        /// <summary>
        /// Writes given sbyte value to given buffer beginning at given offset.
        /// </summary>
        public override void Serialize(object value, byte[] buffer, int offset)
        {
            base.Serialize(value, buffer, offset);
            
            MemoryMapper.WriteSByte((sbyte)value, buffer, offset);
        }
        
        /// <summary>
        /// Reads next 1-bytes from given buffer beginning at given offset as sbyte
        /// and returns that value to the caller.
        /// </summary>
        public override object Deserialize(byte[] buffer, int offset)
        {
            base.Deserialize(buffer, offset);
            
            return MemoryMapper.ReadSByte(buffer, offset);
        }

        /// <summary>
        /// Returns size of sbyte, should always be 1-bytes.
        /// </summary>
        public override ushort GetSizeFromBuffer(byte[] buffer, int offset)
            => sizeof(sbyte);
        
        /// <summary>
        /// Returns size of sbyte, should always be 1-bytes.
        /// </summary>
        public override ushort GetSizeFromValue(object value)
            => sizeof(sbyte);
    }
    
    /// <summary>
    /// Value serializer that provides serialization <see cref="bool"/>.
    /// </summary>
    public sealed class BoolSerializer : ValueSerializer
    {
        public BoolSerializer()
            : base(SerializationType.Bool)
        {
        }
        
        public override bool SupportsType(Type type)
            => type == typeof(bool);

        /// <summary>
        /// Writes given bool value to given buffer beginning at given offset.
        /// </summary>
        public override void Serialize(object value, byte[] buffer, int offset)
        {
            base.Serialize(value, buffer, offset);
            
            MemoryMapper.WriteBool((bool)value, buffer, offset);
        }
        
        /// <summary>
        /// Reads next 1-bytes from given buffer beginning at given offset as bool
        /// and returns that value to the caller.
        /// </summary>
        public override object Deserialize(byte[] buffer, int offset)
        {
            base.Deserialize(buffer, offset);
            
            return MemoryMapper.ReadBool(buffer, offset);
        }

        /// <summary>
        /// Returns size of bool, should always be 1-bytes.
        /// </summary>
        public override ushort GetSizeFromBuffer(byte[] buffer, int offset)
            => sizeof(bool);
        
        /// <summary>
        /// Returns size of bool, should always be 1-bytes.
        /// </summary>
        public override ushort GetSizeFromValue(object value)
            => sizeof(bool);
    }
}