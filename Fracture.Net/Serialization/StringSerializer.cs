using System;
using System.Text;
using Fracture.Common.Memory;

namespace Fracture.Net.Serialization
{
    /// <summary>
    /// Value serializer that provides serialization for <see cref="string"/>. Use .NET default UTF-16 encoding.
    /// </summary>
    public sealed class StringSerializer : ValueSerializer
    {
        #region Static fields
        // UTF-16 encoding.
        private static readonly Encoding Encoding = Encoding.Unicode;
        #endregion
        
        public StringSerializer()
            : base(SerializationType.String)
        {
        }
        
        public override bool SupportsType(Type type)
            => type == typeof(string);
        
        /// <summary>
        /// Writes given string value to given buffer beginning at given offset.
        /// </summary>
        public override void Serialize(object value, byte[] buffer, int offset)
        {
            base.Serialize(value, buffer, offset);
            
            // Get string bytes.
            var bytes = Encoding.GetBytes((string)value);

            // Write the dynamic fields size.
            Protocol.Message.Field.ContentSize.Write((ushort)bytes.Length, buffer, offset);
            
            offset += Protocol.Message.Field.ContentSize.Size;

            // Write the actual data of the string.
            MemoryMapper.VectorizedCopy(bytes, 0, buffer, offset, bytes.Length);
        }
        
        /// <summary>
        /// Reads first 2-bytes from the buffer that contain the size of the string and then proceeds to read the
        /// string itself from given buffer, beginning at given offset and returns the string value to the caller.
        /// </summary>
        public override object Deserialize(byte[] buffer, int offset)
        {
            base.Deserialize(buffer, offset);

            // Get the dynamic field size.
            var size = Protocol.Message.Field.ContentSize.Read(buffer, offset);
            
            offset += Protocol.Message.Field.ContentSize.Size;
            
            // Get the string itself.
            return Encoding.GetString(buffer, offset, size);
        }
        
        /// <summary>
        /// Returns size of the string from the buffer. This consists of the actual string length and the dynamic
        /// field size header size.
        /// </summary>
        public override ushort GetSizeFromBuffer(byte[] buffer, int offset)
            => (ushort)(MemoryMapper.ReadUshort(buffer, offset) + Protocol.Message.Field.ContentSize.Size);

        /// <summary>
        /// Returns size of the string from the value. This consists of the actual string length and the dynamic
        /// field size header size.
        /// </summary>
        public override ushort GetSizeFromValue(object value)
            => (ushort)(Encoding.GetByteCount((string)value) + Protocol.Message.Field.ContentSize.Size);
    }
    
    /// <summary>
    /// Value serializer that provides serialization for <see cref="char"/>. Also servers as serializer
    /// for <see cref="char"/>.
    /// </summary>
    public sealed class CharSerializer : ValueSerializer
    {
        public CharSerializer()
            : base(SerializationType.Char)
        {
        }
        
        public override bool SupportsType(Type type)
            => type == typeof(char);
        
        /// <summary>
        /// Writes given char value to given buffer beginning at given offset.
        /// </summary>
        public override void Serialize(object value, byte[] buffer, int offset)
        {
            base.Serialize(value, buffer, offset);
            
            MemoryMapper.WriteChar((char)value, buffer, offset);
        }
        
        /// <summary>
        /// Reads next 2-bytes from given buffer beginning at given offset as char
        /// and returns that value to the caller.
        /// </summary>
        public override object Deserialize(byte[] buffer, int offset)
        {
            base.Deserialize(buffer, offset);
            
            return MemoryMapper.ReadChar(buffer, offset);
        }

        /// <summary>
        /// Returns size of char, should always be 2-bytes.
        /// </summary>
        public override ushort GetSizeFromBuffer(byte[] buffer, int offset)
            => sizeof(char);
        
        /// <summary>
        /// Returns size of char, should always be 2-bytes.
        /// </summary>
        public override ushort GetSizeFromValue(object value)
            => sizeof(char);
    }
}