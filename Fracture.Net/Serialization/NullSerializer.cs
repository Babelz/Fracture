using System;
using System.Runtime.CompilerServices;
using Fracture.Common.Memory;

namespace Fracture.Net.Serialization
{
    /// <summary>
    /// Value serializer that provides serialization for null values.
    /// </summary>
    public sealed class NullSerializer : ValueSerializer
    {
        public NullSerializer() 
            : base(SerializationType.Null)
        {
        }
        
        public override bool SupportsType(Type type)
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
        public override void Serialize(object value, byte[] buffer, int offset)
        {
            // Sanity check.
            AssertNull(value);

            base.Serialize(null, buffer, offset);
            
            MemoryMapper.WriteByte(0, buffer, offset);
        }

        /// <summary>
        /// Returns size of null from buffer, should always be 1-byte.
        /// </summary>
        public override ushort GetSizeFromBuffer(byte[] buffer, int offset)
            => sizeof(byte);

        /// <summary>
        /// Returns size of null from value, should always be 1-byte.
        /// </summary>
        public override ushort GetSizeFromValue(object value)
        {
            // Sanity check.
            AssertNull(value);
            
            return sizeof(byte);
        }
    }
}