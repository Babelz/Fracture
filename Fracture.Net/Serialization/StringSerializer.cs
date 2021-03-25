using System.Text;
using Fracture.Common.Memory;

namespace Fracture.Net.Serialization
{
    /// <summary>
    /// Value serializer that provides serialization for <see cref="string"/>.
    /// </summary>
    public sealed class StringSerializer : ValueSerializer<string>
    {
        #region Static fields
        private static readonly Encoding Encoding = Encoding.UTF8;
        #endregion
        
        public StringSerializer()
        {
        }
        
        /// <summary>
        /// Writes given string value to given buffer beginning at given offset.
        /// </summary>
        public override void Serialize(object value, byte[] buffer, int offset)
        {
            base.Serialize(value, buffer, offset);
            
            // Get string bytes.
            var bytes = Encoding.GetBytes((string)value);

            // Write the dynamic fields size.
            ByteUtils.WriteUshort((ushort)bytes.Length, buffer, offset);
            
            offset += Protocol.Message.Field.DynamicTypeLengthSize;

            // Write the actual data of the string.
            ByteUtils.VectorizedCopy(bytes, 0, buffer, offset, bytes.Length);
        }
        
        /// <summary>
        /// Reads first 2-bytes from the buffer that contain the size of the string and then proceeds to read the
        /// string itself from given buffer, beginning at given offset and returns the string value to the caller.
        /// </summary>
        public override object Deserialize(byte[] buffer, int offset)
        {
            base.Deserialize(buffer, offset);

            // Get the dynamic field size.
            var size = ByteUtils.ReadUshort(buffer, offset);
            
            offset += Protocol.Message.Field.DynamicTypeLengthSize;
            
            // Get the string itself.
            return Encoding.GetString(buffer, offset, size);
        }
        
        /// <summary>
        /// Returns size of the string from the buffer. This consists of the actual string length and the dynamic
        /// field size header size.
        /// </summary>
        public override ushort GetSizeFromBuffer(byte[] buffer, int offset)
            => (ushort)(ByteUtils.ReadUshort(buffer, offset) + Protocol.Message.Field.DynamicTypeLengthSize);

        /// <summary>
        /// Returns size of the string from the value. This consists of the actual string length and the dynamic
        /// field size header size.
        /// </summary>
        public override ushort GetSizeFromValue(object value)
            => (ushort)(Encoding.GetByteCount((string)value) + Protocol.Message.Field.DynamicTypeLengthSize);
    }
}