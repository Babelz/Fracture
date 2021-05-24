using System;
using Fracture.Common.Memory;

namespace Fracture.Net.Serialization
{
    /// <summary>
    /// Value serializer that provides serialization for <see cref="byte"/>.
    /// </summary>
    public sealed class ByteSerializer : IValueSerializer
    {
        public ByteSerializer()
        {
        }
        
        public bool SupportsType(Type type)
            => type == typeof(byte);

        /// <summary>
        /// Writes given byte value to given buffer beginning at given offset.
        /// </summary>
        public void Serialize(object value, byte[] buffer, int offset)
        {
            MemoryMapper.WriteByte((byte)value, buffer, offset);
        }
        
        /// <summary>
        /// Reads next 1-bytes from given buffer beginning at given offset as byte
        /// and returns that value to the caller.
        /// </summary>
        public object Deserialize(byte[] buffer, int offset)
        {
            return MemoryMapper.ReadByte(buffer, offset);
        }

        /// <summary>
        /// Returns size of byte, should always be 1-bytes.
        /// </summary>
        public ushort GetSizeFromBuffer(byte[] buffer, int offset)
            => sizeof(byte);
        
        /// <summary>
        /// Returns size of byte, should always be 1-bytes.
        /// </summary>
        public ushort GetSizeFromValue(object value)
            => sizeof(byte);
    }
    
    /// <summary>
    /// Value serializer that provides serialization for <see cref="sbyte"/>.
    /// </summary>
    public sealed class SbyteSerializer : IValueSerializer
    {
        public SbyteSerializer()
        {
        }
        
        public bool SupportsType(Type type)
            => type == typeof(sbyte);

        /// <summary>
        /// Writes given sbyte value to given buffer beginning at given offset.
        /// </summary>
        public void Serialize(object value, byte[] buffer, int offset)
        {
            MemoryMapper.WriteSByte((sbyte)value, buffer, offset);
        }
        
        /// <summary>
        /// Reads next 1-bytes from given buffer beginning at given offset as sbyte
        /// and returns that value to the caller.
        /// </summary>
        public object Deserialize(byte[] buffer, int offset)
        {
            return MemoryMapper.ReadSByte(buffer, offset);
        }

        /// <summary>
        /// Returns size of sbyte, should always be 1-bytes.
        /// </summary>
        public ushort GetSizeFromBuffer(byte[] buffer, int offset)
            => sizeof(sbyte);
        
        /// <summary>
        /// Returns size of sbyte, should always be 1-bytes.
        /// </summary>
        public ushort GetSizeFromValue(object value)
            => sizeof(sbyte);
    }
    
    /// <summary>
    /// Value serializer that provides serialization <see cref="bool"/>.
    /// </summary>
    public sealed class BoolSerializer : IValueSerializer
    {
        public BoolSerializer()
        {
        }
        
        public bool SupportsType(Type type)
            => type == typeof(bool);

        /// <summary>
        /// Writes given bool value to given buffer beginning at given offset.
        /// </summary>
        public void Serialize(object value, byte[] buffer, int offset)
        {
            MemoryMapper.WriteBool((bool)value, buffer, offset);
        }
        
        /// <summary>
        /// Reads next 1-bytes from given buffer beginning at given offset as bool
        /// and returns that value to the caller.
        /// </summary>
        public object Deserialize(byte[] buffer, int offset)
        {
            return MemoryMapper.ReadBool(buffer, offset);
        }

        /// <summary>
        /// Returns size of bool, should always be 1-bytes.
        /// </summary>
        public ushort GetSizeFromBuffer(byte[] buffer, int offset)
            => sizeof(bool);
        
        /// <summary>
        /// Returns size of bool, should always be 1-bytes.
        /// </summary>
        public ushort GetSizeFromValue(object value)
            => sizeof(bool);
    }
}