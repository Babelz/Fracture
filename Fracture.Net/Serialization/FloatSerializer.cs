using Fracture.Common.Memory;

namespace Fracture.Net.Serialization
{
    /// <summary>
    /// Value serializer that provides serialization for <see cref="float"/>.
    /// </summary>
    public sealed class FloatSerializer : ValueSerializer
    {
        public FloatSerializer()
            : base(SerializationType.Float, typeof(float))
        {
        }
        
        /// <summary>
        /// Writes given float value to given buffer beginning at given offset.
        /// </summary>
        public override void Serialize(object value, byte[] buffer, int offset)
        {
            base.Serialize(value, buffer, offset);
            
            ByteUtils.WriteFloat((float)value, buffer, offset);
        }
        
        /// <summary>
        /// Reads next 4-bytes from given buffer beginning at given offset as float
        /// and returns that value to the caller.
        /// </summary>
        public override object Deserialize(byte[] buffer, int offset)
        {
            base.Deserialize(buffer, offset);

            return ByteUtils.ReadFloat(buffer, offset);
        }
        
        /// <summary>
        /// Returns size of float, should always be 4-bytes.
        /// </summary>
        public override ushort GetSizeFromBuffer(byte[] buffer, int offset)
            => sizeof(float);

        /// <summary>
        /// Returns size of float, should always be 4-bytes.
        /// </summary>
        public override ushort GetSizeFromValue(object value)
            => sizeof(float);
    }
    
    /// <summary>
    /// Value serializer that provides serialization for <see cref="double"/>.
    /// </summary>
    public sealed class DoubleSerializer : ValueSerializer
    {
        public DoubleSerializer()
            : base(SerializationType.Double, typeof(double))
        {
        }
        
        /// <summary>
        /// Writes given double value to given buffer beginning at given offset.
        /// </summary>
        public override void Serialize(object value, byte[] buffer, int offset)
        {
            base.Serialize(value, buffer, offset);
            
            ByteUtils.WriteDouble((double)value, buffer, offset);
        }
        
        /// <summary>
        /// Reads next 8-bytes from given buffer beginning at given offset as double
        /// and returns that value to the caller.
        /// </summary>
        public override object Deserialize(byte[] buffer, int offset)
        {
            base.Deserialize(buffer, offset);

            return ByteUtils.ReadDouble(buffer, offset);
        }
        
        /// <summary>
        /// Returns size of double, should always be 8-bytes.
        /// </summary>
        public override ushort GetSizeFromBuffer(byte[] buffer, int offset)
            => sizeof(double);

        /// <summary>
        /// Returns size of double, should always be 8-bytes.
        /// </summary>
        public override ushort GetSizeFromValue(object value)
            => sizeof(double);
    }
}