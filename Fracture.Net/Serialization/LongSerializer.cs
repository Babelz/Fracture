using Fracture.Common.Memory;

namespace Fracture.Net.Serialization
{
    /// <summary>
    /// Value serializer that provides serialization for <see cref="long"/>.
    /// </summary>
    public sealed class LongSerializer : ValueSerializer
    {
        public LongSerializer()
            : base(SerializationType.Long, typeof(long))
        {
        }
        
        /// <summary>
        /// Writes given int64 value to given buffer beginning at given offset.
        /// </summary>
        public override void Serialize(object value, byte[] buffer, int offset)
        {
            base.Serialize(value, buffer, offset);
            
            ByteUtils.WriteLong((long)value, buffer, offset);
        }
        
        /// <summary>
        /// Reads next 8-bytes from given buffer beginning at given offset as int32
        /// and returns that value to the caller.
        /// </summary>
        public override object Deserialize(byte[] buffer, int offset)
        {
            base.Deserialize(buffer, offset);
            
            return ByteUtils.ReadLong(buffer, offset);
        }

        /// <summary>
        /// Returns size of int64, should always be 8-bytes.
        /// </summary>
        public override ushort GetSizeFromBuffer(byte[] buffer, int offset)
            => sizeof(long);
        
        /// <summary>
        /// Returns size of int64, should always be 8-bytes.
        /// </summary>
        public override ushort GetSizeFromValue(object value)
            => sizeof(long);
    }
    
    /// <summary>
    /// Value serializer that provides serialization for <see cref="ulong"/>.
    /// </summary>
    public sealed class UlongSerializer : ValueSerializer
    {
        public UlongSerializer()
            : base(SerializationType.Ulong, typeof(ulong))
        {
        }
        
        /// <summary>
        /// Writes given uint64 value to given buffer beginning at given offset.
        /// </summary>
        public override void Serialize(object value, byte[] buffer, int offset)
        {
            base.Serialize(value, buffer, offset);
            
            ByteUtils.WriteUlong((ulong)value, buffer, offset);
        }
        
        /// <summary>
        /// Reads next 8-bytes from given buffer beginning at given offset as uint32
        /// and returns that value to the caller.
        /// </summary>
        public override object Deserialize(byte[] buffer, int offset)
        {
            base.Deserialize(buffer, offset);
            
            return ByteUtils.ReadUlong(buffer, offset);
        }

        /// <summary>
        /// Returns size of uint64, should always be 8-bytes.
        /// </summary>
        public override ushort GetSizeFromBuffer(byte[] buffer, int offset)
            => sizeof(ulong);
        
        /// <summary>
        /// Returns size of uint64, should always be 8-bytes.
        /// </summary>
        public override ushort GetSizeFromValue(object value)
            => sizeof(ulong);
    }
}