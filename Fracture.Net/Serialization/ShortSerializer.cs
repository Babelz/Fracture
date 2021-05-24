using System;
using Fracture.Common.Memory;

namespace Fracture.Net.Serialization
{
    /// <summary>
    /// Value serializer that provides serialization for <see cref="short"/>.
    /// </summary>
    public sealed class ShortSerializer : IValueSerializer
    {
        public ShortSerializer()
        {
        }

        public bool SupportsType(Type type)
            => type == typeof(short);
        
        /// <summary>
        /// Writes given int16 value to given buffer beginning at given offset.
        /// </summary>
        public void Serialize(object value, byte[] buffer, int offset)
        {
            MemoryMapper.WriteShort((short)value, buffer, offset);
        }
        
        /// <summary>
        /// Reads next 2-bytes from given buffer beginning at given offset as int16
        /// and returns that value to the caller.
        /// </summary>
        public object Deserialize(byte[] buffer, int offset)
        {
            return MemoryMapper.ReadShort(buffer, offset);
        }

        /// <summary>
        /// Returns size of int16, should always be 2-bytes.
        /// </summary>
        public ushort GetSizeFromBuffer(byte[] buffer, int offset)
            => sizeof(short);
        
        /// <summary>
        /// Returns size of int16, should always be 2-bytes.
        /// </summary>
        public ushort GetSizeFromValue(object value)
            => sizeof(short);
    }
    
    /// <summary>
    /// Value serializer that provides serialization for <see cref="ushort"/>.
    /// </summary>
    public sealed class UshortSerializer : IValueSerializer
    {
        public UshortSerializer()
        {
        }
        
        public bool SupportsType(Type type)
            => type == typeof(ushort);
        
        /// <summary>
        /// Writes given uint16 value to given buffer beginning at given offset.
        /// </summary>
        public void Serialize(object value, byte[] buffer, int offset)
        {
            MemoryMapper.WriteUshort((ushort)value, buffer, offset);
        }
        
        /// <summary>
        /// Reads next 2-bytes from given buffer beginning at given offset as uint16
        /// and returns that value to the caller.
        /// </summary>
        public object Deserialize(byte[] buffer, int offset)
        {
            return MemoryMapper.ReadUshort(buffer, offset);
        }

        /// <summary>
        /// Returns size of uint16, should always be 2-bytes.
        /// </summary>
        public ushort GetSizeFromBuffer(byte[] buffer, int offset)
            => sizeof(ushort);
        
        /// <summary>
        /// Returns size of uint16, should always be 2-bytes.
        /// </summary>
        public ushort GetSizeFromValue(object value)
            => sizeof(ushort);
    }
}