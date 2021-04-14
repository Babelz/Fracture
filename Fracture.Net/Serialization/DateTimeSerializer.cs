using System;
using System.Data;
using Fracture.Common.Memory;

namespace Fracture.Net.Serialization
{
    /// <summary>
    /// Value serializer that provides serialization for <see cref="TimeSpan"/>.
    /// </summary>
    public sealed class TimeSpanSerializer : ValueSerializer
    {
        public TimeSpanSerializer() 
            : base(SerializationType.TimeSpan)
        {
        }
        
        public override bool SupportsType(Type type)
            => type == typeof(TimeSpan);

        /// <summary>
        /// Writes given time span value to given buffer beginning at given offset.
        /// </summary>
        public override void Serialize(object value, byte[] buffer, int offset)
        {
            base.Serialize(value, buffer, offset);
            
            ByteUtils.WriteLong(((TimeSpan)value).Ticks, buffer, offset);
        }
        
        /// <summary>
        /// Reads next 8-bytes from given buffer beginning at given offset as time span
        /// and returns that value to the caller.
        /// </summary>
        public override object Deserialize(byte[] buffer, int offset)
        {
            base.Deserialize(buffer, offset);
            
            return new TimeSpan(ByteUtils.ReadLong(buffer, offset));
        }
        
        /// <summary>
        /// Returns size of time span, should always be 8-bytes.
        /// </summary>
        public override ushort GetSizeFromBuffer(byte[] buffer, int offset)
            => sizeof(long);

        /// <summary>
        /// Returns size of time span, should always be 8-bytes.
        /// </summary>
        public override ushort GetSizeFromValue(object value)
            => sizeof(long);
    }
    
    /// <summary>
    /// Value serializer that provides serialization for <see cref="DateTime"/>.
    /// </summary>
    public sealed class DateTimeSerializer : ValueSerializer
    {
        #region Static fields
        private static readonly long MaxTicks = DateTime.MaxValue.Ticks;
        private static readonly long MinTicks = DateTime.MinValue.Ticks;
        #endregion
        
        public DateTimeSerializer() 
            : base(SerializationType.DateTime)
        {
        }
        
        public override bool SupportsType(Type type)
            => type == typeof(DateTime);

        /// <summary>
        /// Writes given date time value to given buffer beginning at given offset.
        /// </summary>
        public override void Serialize(object value, byte[] buffer, int offset)
        {
            base.Serialize(value, buffer, offset);
            
            ByteUtils.WriteLong(((DateTime)value).Ticks, buffer, offset);
        }
        
        /// <summary>
        /// Reads next 8-bytes from given buffer beginning at given offset as date time
        /// and returns that value to the caller.
        /// </summary>
        public override object Deserialize(byte[] buffer, int offset)
        {
            base.Deserialize(buffer, offset);
            
            var ticks = ByteUtils.ReadLong(buffer, offset);
            
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
        public override ushort GetSizeFromBuffer(byte[] buffer, int offset)
            => sizeof(long);

        /// <summary>
        /// Returns size of date time, should always be 8-bytes.
        /// </summary>
        public override ushort GetSizeFromValue(object value)
            => sizeof(long);
    }
}