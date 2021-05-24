using System;
using System.Runtime.CompilerServices;
using Fracture.Common.Memory;

namespace Fracture.Net.Serialization
{
    /// <summary>
    /// Value serializer that provides serialization for null values.
    /// </summary>
    public sealed class NullSerializer : IValueSerializer
    {
        public NullSerializer()
        {
        }
        
        public bool SupportsType(Type type)
            => type == null;
        
        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        /// <summary>
        /// Sanity check to ensure that the serialize is actually used for serializing null values.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AssertNull(object value)
        {
            if (value != null)
                throw new InvalidOperationException("expecting null value");
        }
        
        /// <summary>
        /// Writes zero byte to given buffer at given offset.
        /// </summary>
        public void Serialize(object value, byte[] buffer, int offset)
        {
            // Sanity check.
            AssertNull(value);
            
            MemoryMapper.WriteByte(0, buffer, offset);
        }

        /// <summary>
        /// Returns constant null.
        /// </summary>
        public object Deserialize(byte[] buffer, int offset)
        {
            return null;
        }

        /// <summary>
        /// Returns size of null from buffer, should always be 1-byte.
        /// </summary>
        public ushort GetSizeFromBuffer(byte[] buffer, int offset)
            => sizeof(byte);

        /// <summary>
        /// Returns size of null from value, should always be 1-byte.
        /// </summary>
        public ushort GetSizeFromValue(object value)
        {
            // Sanity check.
            AssertNull(value);
            
            return sizeof(byte);
        }
    }
}