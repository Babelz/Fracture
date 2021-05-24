using System;
using Fracture.Common.Memory;

namespace Fracture.Net.Serialization
{
    /// <summary>
    /// Value serializer that provides serialization for <see cref="TimeSpan"/>.
    /// </summary>
    public sealed class TimeSpanSerializer : IValueSerializer
    {
        public TimeSpanSerializer()
        {
        }
        
        public bool SupportsType(Type type)
            => type == typeof(TimeSpan);

        /// <summary>
        /// Writes given time span value to given buffer beginning at given offset.
        /// </summary>
        public void Serialize(object value, byte[] buffer, int offset)
        {
            MemoryMapper.WriteLong(((TimeSpan)value).Ticks, buffer, offset);
        }
        
        /// <summary>
        /// Reads next 8-bytes from given buffer beginning at given offset as time span
        /// and returns that value to the caller.
        /// </summary>
        public object Deserialize(byte[] buffer, int offset)
        {
            return new TimeSpan(MemoryMapper.ReadLong(buffer, offset));
        }
        
        /// <summary>
        /// Returns size of time span, should always be 8-bytes.
        /// </summary>
        public ushort GetSizeFromBuffer(byte[] buffer, int offset)
            => sizeof(long);

        /// <summary>
        /// Returns size of time span, should always be 8-bytes.
        /// </summary>
        public ushort GetSizeFromValue(object value)
            => sizeof(long);
    }
    
    /// <summary>
    /// Value serializer that provides serialization for <see cref="DateTime"/>.
    /// </summary>
    public sealed class DateTimeSerializer : IValueSerializer
    {
        #region Static fields
        private static readonly long MaxTicks = DateTime.MaxValue.Ticks;
        private static readonly long MinTicks = DateTime.MinValue.Ticks;
        #endregion
        
        public DateTimeSerializer()
        {
        }
        
        public bool SupportsType(Type type)
            => type == typeof(DateTime);

        /// <summary>
        /// Writes given date time value to given buffer beginning at given offset.
        /// </summary>
        public void Serialize(object value, byte[] buffer, int offset)
        {
            MemoryMapper.WriteLong(((DateTime)value).Ticks, buffer, offset);
        }
        
        /// <summary>
        /// Reads next 8-bytes from given buffer beginning at given offset as date time
        /// and returns that value to the caller.
        /// </summary>
        public object Deserialize(byte[] buffer, int offset)
        {
            var ticks = MemoryMapper.ReadLong(buffer, offset);
            
            // Clamp ticks to fit max representable date time value.
            if (ticks > 0)
                ticks = ticks > MaxTicks ? MaxTicks : ticks;
            else 
                ticks = ticks < MinTicks ? MinTicks : ticks;
                
            return new DateTime(ticks);
        }
        
        /// <summary>
        /// Returns size of date time, should always be 8-bytes.
        /// </summary>
        public ushort GetSizeFromBuffer(byte[] buffer, int offset)
            => sizeof(long);

        /// <summary>
        /// Returns size of date time, should always be 8-bytes.
        /// </summary>
        public ushort GetSizeFromValue(object value)
            => sizeof(long);
    }
}