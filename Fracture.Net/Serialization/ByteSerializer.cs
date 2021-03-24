using Fracture.Common.Memory;

namespace Fracture.Net.Serialization
{
    /// <summary>
    /// Value serializer that provides serialization for <see cref="byte"/>.
    /// </summary>
    public sealed class ByteSerializer : ValueSerializer<byte>
    {
        public ByteSerializer()
        {
        }
        
        /// <summary>
        /// Writes given byte value to given buffer beginning at given offset.
        /// </summary>
        public override void Serialize(object value, byte[] buffer, int offset)
        {
            base.Serialize(value, buffer, offset);
            
            ByteUtils.WriteByte((byte)value, buffer, offset);
        }
        
        /// <summary>
        /// Reads next 1-bytes from given buffer beginning at given offset as byte
        /// and returns that value to the caller.
        /// </summary>
        public override object Deserialize(byte[] buffer, int offset)
        {
            base.Deserialize(buffer, offset);
            
            return ByteUtils.ReadByte(buffer, offset);
        }

        /// <summary>
        /// Returns size of byte, should always be 1-bytes.
        /// </summary>
        public override int GetSizeFromBuffer(byte[] buffer, int offset)
            => sizeof(byte);
        
        /// <summary>
        /// Returns size of byte, should always be 1-bytes.
        /// </summary>
        public override int GetSizeFromValue(object value)
            => sizeof(byte);
    }
    
    /// <summary>
    /// Value serializer that provides serialization for <see cref="sbyte"/>.
    /// </summary>
    public sealed class SbyteSerializer : ValueSerializer<sbyte>
    {
        public SbyteSerializer()
        {
        }
        
        /// <summary>
        /// Writes given sbyte value to given buffer beginning at given offset.
        /// </summary>
        public override void Serialize(object value, byte[] buffer, int offset)
        {
            base.Serialize(value, buffer, offset);
            
            ByteUtils.WriteSByte((sbyte)value, buffer, offset);
        }
        
        /// <summary>
        /// Reads next 1-bytes from given buffer beginning at given offset as sbyte
        /// and returns that value to the caller.
        /// </summary>
        public override object Deserialize(byte[] buffer, int offset)
        {
            base.Deserialize(buffer, offset);
            
            return ByteUtils.ReadSByte(buffer, offset);
        }

        /// <summary>
        /// Returns size of sbyte, should always be 1-bytes.
        /// </summary>
        public override int GetSizeFromBuffer(byte[] buffer, int offset)
            => sizeof(sbyte);
        
        /// <summary>
        /// Returns size of sbyte, should always be 1-bytes.
        /// </summary>
        public override int GetSizeFromValue(object value)
            => sizeof(sbyte);
    }
}