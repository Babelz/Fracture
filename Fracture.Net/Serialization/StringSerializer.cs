using System;
using System.Text;
using Fracture.Common.Memory;

namespace Fracture.Net.Serialization
{
    /// <summary>
    /// Value serializer that provides serialization for <see cref="string"/>. Use .NET default UTF-16 encoding.
    /// </summary>
    [ValueSerializer(typeof(string))]
    public static class StringSerializer
    {
        #region Static fields
        // UTF-16 encoding.
        private static readonly Encoding Encoding = Encoding.Unicode;
        #endregion

        [ValueSerializer.SupportsType]
        public static bool SupportsType(Type type)
            => type == typeof(string);

        /// <summary>
        /// Writes given string value to given buffer beginning at given offset.
        /// </summary>
        [ValueSerializer.Serialize]
        public static void Serialize(string value, byte [] buffer, int offset)
        {
            // Get string bytes.
            var bytes = Encoding.GetBytes(value);

            // Write the dynamic fields size. Include header size to content length.
            Protocol.ContentLength.Write(checked((ushort)(bytes.Length + Protocol.ContentLength.Size)), buffer, offset);

            offset += Protocol.ContentLength.Size;

            // Write the actual data of the string.
            MemoryMapper.VectorizedCopy(bytes, 0, buffer, offset, bytes.Length);
        }

        /// <summary>
        /// Reads first 2-bytes from the buffer that contain the size of the string and then proceeds to read the
        /// string itself from given buffer, beginning at given offset and returns the string value to the caller.
        /// </summary>
        [ValueSerializer.Deserialize]
        public static string Deserialize(byte [] buffer, int offset)
        {
            // Get the dynamic field size. Remove content length bytes count from the actual size to get the string length.
            var size = checked((ushort)(Protocol.ContentLength.Read(buffer, offset) - Protocol.ContentLength.Size));
            offset += Protocol.ContentLength.Size;

            // Get the string itself.
            return Encoding.GetString(buffer, offset, size);
        }

        /// <summary>
        /// Returns size of the string from the buffer. This consists of the actual string length and the dynamic
        /// field size header size.
        /// </summary>
        [ValueSerializer.GetSizeFromBuffer]
        public static ushort GetSizeFromBuffer(byte [] buffer, int offset)
            => Protocol.ContentLength.Read(buffer, offset);

        /// <summary>
        /// Returns size of the string from the value. This consists of the actual string length and the dynamic
        /// field size header size.
        /// </summary>
        [ValueSerializer.GetSizeFromValue]
        public static ushort GetSizeFromValue(string value)
            => checked((ushort)(Encoding.GetByteCount(value) + Protocol.ContentLength.Size));
    }

    /// <summary>
    /// Value serializer that provides serialization for <see cref="char"/>. Also servers as serializer
    /// for <see cref="char"/>.
    /// </summary>
    [ValueSerializer(typeof(char))]
    public static class CharSerializer
    {
        [ValueSerializer.SupportsType]
        public static bool SupportsType(Type type)
            => type == typeof(char);

        /// <summary>
        /// Writes given char value to given buffer beginning at given offset.
        /// </summary>
        [ValueSerializer.Serialize]
        public static void Serialize(char value, byte [] buffer, int offset)
            => MemoryMapper.WriteChar(value, buffer, offset);

        /// <summary>
        /// Reads next 2-bytes from given buffer beginning at given offset as char
        /// and returns that value to the caller.
        /// </summary>
        [ValueSerializer.Deserialize]
        public static char Deserialize(byte [] buffer, int offset)
            => MemoryMapper.ReadChar(buffer, offset);

        /// <summary>
        /// Returns size of char, should always be 2-bytes.
        /// </summary>
        [ValueSerializer.GetSizeFromBuffer]
        public static ushort GetSizeFromBuffer(byte [] buffer, int offset)
            => sizeof(char);

        /// <summary>
        /// Returns size of char, should always be 2-bytes.
        /// </summary>
        [ValueSerializer.GetSizeFromValue]
        public static ushort GetSizeFromValue(char value)
            => sizeof(char);
    }
}