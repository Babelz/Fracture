using System;
using Fracture.Common.Memory;

namespace Fracture.Net.Serialization
{
    /// <summary>
    /// Value serializer that provides serialization for <see cref="float"/>.
    /// </summary>
    public sealed class FloatSerializer : IValueSerializer
    {
        public FloatSerializer()
        {
        }
        
        public bool SupportsType(Type type)
            => type == typeof(float);
        
        /// <summary>
        /// Writes given float value to given buffer beginning at given offset.
        /// </summary>
        public void Serialize(object value, byte[] buffer, int offset)
        {
            MemoryMapper.WriteFloat((float)value, buffer, offset);
        }
        
        /// <summary>
        /// Reads next 4-bytes from given buffer beginning at given offset as float
        /// and returns that value to the caller.
        /// </summary>
        public object Deserialize(byte[] buffer, int offset)
        {
            return MemoryMapper.ReadFloat(buffer, offset);
        }
        
        /// <summary>
        /// Returns size of float, should always be 4-bytes.
        /// </summary>
        public ushort GetSizeFromBuffer(byte[] buffer, int offset)
            => sizeof(float);

        /// <summary>
        /// Returns size of float, should always be 4-bytes.
        /// </summary>
        public ushort GetSizeFromValue(object value)
            => sizeof(float);
    }
    
    /// <summary>
    /// Value serializer that provides serialization for <see cref="double"/>.
    /// </summary>
    public sealed class DoubleSerializer : IValueSerializer
    {
        public DoubleSerializer()
        {
        }
        
        public bool SupportsType(Type type)
            => type == typeof(double);
        
        /// <summary>
        /// Writes given double value to given buffer beginning at given offset.
        /// </summary>
        public void Serialize(object value, byte[] buffer, int offset)
        {
            MemoryMapper.WriteDouble((double)value, buffer, offset);
        }
        
        /// <summary>
        /// Reads next 8-bytes from given buffer beginning at given offset as double
        /// and returns that value to the caller.
        /// </summary>
        public object Deserialize(byte[] buffer, int offset)
        {
            return MemoryMapper.ReadDouble(buffer, offset);
        }
        
        /// <summary>
        /// Returns size of double, should always be 8-bytes.
        /// </summary>
        public ushort GetSizeFromBuffer(byte[] buffer, int offset)
            => sizeof(double);

        /// <summary>
        /// Returns size of double, should always be 8-bytes.
        /// </summary>
        public ushort GetSizeFromValue(object value)
            => sizeof(double);
    }
}