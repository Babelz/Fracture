using System;
using System.Text;
using Fracture.Common.Memory;

namespace Fracture.Net.Serialization
{
    /// <summary>
    /// Value serializer that provides serialization for <see cref="string"/>. Use .NET default UTF-16 encoding.
    /// </summary>
    public sealed class StringSerializer : IValueSerializer
    {
        #region Static fields
        // UTF-16 encoding.
        private static readonly Encoding Encoding = Encoding.Unicode;
        #endregion
        
        public StringSerializer()
        {
        }
        
        public bool SupportsType(Type type)
            => type == typeof(string); 
        
        /// <summary>
        /// Writes given string value to given buffer beginning at given offset.
        /// </summary>
        public void Serialize(object value, byte[] buffer, int offset)
        {
            // Get string bytes.
            var bytes = Encoding.GetBytes((string)value);

            // Write the dynamic fields size.
            Protocol.Value.ContentSize.Write((ushort)bytes.Length, buffer, offset);
            
            offset += Protocol.Value.ContentSize.Size;

            // Write the actual data of the string.
            MemoryMapper.VectorizedCopy(bytes, 0, buffer, offset, bytes.Length);
        }
        
        /// <summary>
        /// Reads first 2-bytes from the buffer that contain the size of the string and then proceeds to read the
        /// string itself from given buffer, beginning at given offset and returns the string value to the caller.
        /// </summary>
        public object Deserialize(byte[] buffer, int offset)
        {
            // Get the dynamic field size.
            var size = Protocol.Value.ContentSize.Read(buffer, offset);
            
            offset += Protocol.Value.ContentSize.Size;
            
            // Get the string itself.
            return Encoding.GetString(buffer, offset, size);
        }
        
        /// <summary>
        /// Returns size of the string from the buffer. This consists of the actual string length and the dynamic
        /// field size header size.
        /// </summary>
        public ushort GetSizeFromBuffer(byte[] buffer, int offset)
            => (ushort)(MemoryMapper.ReadUshort(buffer, offset) + Protocol.Value.ContentSize.Size);

        /// <summary>
        /// Returns size of the string from the value. This consists of the actual string length and the dynamic
        /// field size header size.
        /// </summary>
        public ushort GetSizeFromValue(object value)
            => (ushort)(Encoding.GetByteCount((string)value) + Protocol.Value.ContentSize.Size);
    }
    
    /// <summary>
    /// Value serializer that provides serialization for <see cref="char"/>. Also servers as serializer
    /// for <see cref="char"/>.
    /// </summary>
    public sealed class CharSerializer : IValueSerializer
    {
        public CharSerializer()
        {
        }
        
        public bool SupportsType(Type type)
            => type == typeof(char);
        
        /// <summary>
        /// Writes given char value to given buffer beginning at given offset.
        /// </summary>
        public void Serialize(object value, byte[] buffer, int offset)
        {
            MemoryMapper.WriteChar((char)value, buffer, offset);
        }
        
        /// <summary>
        /// Reads next 2-bytes from given buffer beginning at given offset as char
        /// and returns that value to the caller.
        /// </summary>
        public object Deserialize(byte[] buffer, int offset)
        {
            return MemoryMapper.ReadChar(buffer, offset);
        }

        /// <summary>
        /// Returns size of char, should always be 2-bytes.
        /// </summary>
        public ushort GetSizeFromBuffer(byte[] buffer, int offset)
            => sizeof(char);
        
        /// <summary>
        /// Returns size of char, should always be 2-bytes.
        /// </summary>
        public ushort GetSizeFromValue(object value)
            => sizeof(char);
    }
}