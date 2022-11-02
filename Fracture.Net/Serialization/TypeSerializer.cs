using System;
using System.Text;
using Fracture.Common.Memory;

namespace Fracture.Net.Serialization
{
    /// <summary>
    /// Value serializer that provides serialization for <see cref="System.Type"/>.
    /// </summary>
    [ValueSerializer(typeof(Type))]
    public static class TypeSerializer
    {
        #region Static fields
        private static readonly Encoding Encoding = Encoding.ASCII;
        #endregion

        [ValueSerializer.SupportsType]
        public static bool SupportsType(Type type)
            => type == typeof(Type);

        /// <summary>
        /// Writes given string value to given buffer beginning at given offset.
        /// </summary>
        [ValueSerializer.Serialize]
        public static void Serialize(Type value, byte[] buffer, int offset)
            => StringSerializer.Serialize(value.AssemblyQualifiedName, buffer, offset);

        /// <summary>
        /// Reads first 2-bytes from the buffer that contain the size of the type and then proceeds to read the
        /// type itself from given buffer, beginning at given offset and returns the type value to the caller.
        /// </summary>
        [ValueSerializer.Deserialize]
        public static Type Deserialize(byte[] buffer, int offset)
            => Type.GetType(StringSerializer.Deserialize(buffer, offset));

        /// <summary>
        /// Returns size of the type from the buffer. This consists of the actual type length and the dynamic
        /// field size header size.
        /// </summary>
        [ValueSerializer.GetSizeFromBuffer]
        public static ushort GetSizeFromBuffer(byte[] buffer, int offset)
            => StringSerializer.GetSizeFromBuffer(buffer, offset);

        /// <summary>
        /// Returns size of the type from the value. This consists of the actual type length and the dynamic
        /// field size header size.
        /// </summary>
        [ValueSerializer.GetSizeFromValue]
        public static ushort GetSizeFromValue(Type value)
            => StringSerializer.GetSizeFromValue(value.AssemblyQualifiedName);
    }
}