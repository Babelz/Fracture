using System;
using Fracture.Common.Memory;

namespace Fracture.Net.Serialization
{
    /// <summary>
    /// Value serializer that provides serialization for <see cref="short"/>.
    /// </summary>
    public sealed class ShortSerializer : ValueSerializer
    {
        public ShortSerializer()
            : base(SerializationType.Short)
        {
        }

        public override bool SupportsType(Type type)
            => type == typeof(short);
        
        /// <summary>
        /// Writes given int16 value to given buffer beginning at given offset.
        /// </summary>
        public override void Serialize(object value, byte[] buffer, int offset)
        {
            base.Serialize(value, buffer, offset);
            
            ByteUtils.WriteShort((short)value, buffer, offset);
        }
        
        /// <summary>
        /// Reads next 2-bytes from given buffer beginning at given offset as int16
        /// and returns that value to the caller.
        /// </summary>
        public override object Deserialize(byte[] buffer, int offset)
        {
            base.Deserialize(buffer, offset);
            
            return ByteUtils.ReadShort(buffer, offset);
        }

        /// <summary>
        /// Returns size of int16, should always be 2-bytes.
        /// </summary>
        public override ushort GetSizeFromBuffer(byte[] buffer, int offset)
            => sizeof(short);
        
        /// <summary>
        /// Returns size of int16, should always be 2-bytes.
        /// </summary>
        public override ushort GetSizeFromValue(object value)
            => sizeof(short);
    }
    
    /// <summary>
    /// Value serializer that provides serialization for <see cref="ushort"/>.
    /// </summary>
    public sealed class UshortSerializer : ValueSerializer
    {
        public UshortSerializer()
            : base(SerializationType.Ushort)
        {
        }
        
        public override bool SupportsType(Type type)
            => type == typeof(ushort);
        
        /// <summary>
        /// Writes given uint16 value to given buffer beginning at given offset.
        /// </summary>
        public override void Serialize(object value, byte[] buffer, int offset)
        {
            base.Serialize(value, buffer, offset);
            
            ByteUtils.WriteUshort((ushort)value, buffer, offset);
        }
        
        /// <summary>
        /// Reads next 2-bytes from given buffer beginning at given offset as uint16
        /// and returns that value to the caller.
        /// </summary>
        public override object Deserialize(byte[] buffer, int offset)
        {
            base.Deserialize(buffer, offset);
            
            return ByteUtils.ReadUshort(buffer, offset);
        }

        /// <summary>
        /// Returns size of uint16, should always be 2-bytes.
        /// </summary>
        public override ushort GetSizeFromBuffer(byte[] buffer, int offset)
            => sizeof(ushort);
        
        /// <summary>
        /// Returns size of uint16, should always be 2-bytes.
        /// </summary>
        public override ushort GetSizeFromValue(object value)
            => sizeof(ushort);
    }
}