using System;
using System.IO;
using System.Runtime.Remoting.Messaging;
using Fracture.Common.Memory;

namespace Fracture.Net.Serialization
{
    /// <summary>
    /// Value serializer that provides serialization for <see cref="float"/>.
    /// </summary>
    public sealed class FloatSerializer : ValueSerializer<float>
    {
        public FloatSerializer()
        {
        }
        
        /// <summary>
        /// Writes given float value to given buffer beginning at given offset.
        /// </summary>
        public override void Serialize(object value, byte[] buffer, int offset)
        {
            CheckLowerBound(buffer.Length, offset, sizeof(float));
            CheckUpperBound(buffer.Length, offset, sizeof(float));
            
            ByteUtils.WriteFloat((float)value, buffer, offset);
        }
        
        /// <summary>
        /// Reads next 4-bytes from given buffer beginning at given offset as float
        /// and returns that value to the caller.
        /// </summary>
        public override object Deserialize(byte[] buffer, int offset)
        {
            CheckLowerBound(buffer.Length, offset, sizeof(float));
            CheckUpperBound(buffer.Length, offset, sizeof(float));

            return ByteUtils.ReadFloat(buffer, offset);
        }
        
        /// <summary>
        /// Returns size of float, should always be 4-bytes.
        /// </summary>
        public override int GetSizeFromBuffer(byte[] buffer, int offset)
            => sizeof(float);

        /// <summary>
        /// Returns size of float, should always be 4-bytes.
        /// </summary>
        public override int GetSizeFromValue(object value)
            => sizeof(float);
    }
}