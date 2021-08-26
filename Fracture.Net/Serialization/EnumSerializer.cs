using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Xml.Schema;
using Fracture.Common.Memory;
using Fracture.Common.Reflection;
using Perfolizer.Mathematics.RangeEstimators;

namespace Fracture.Net.Serialization
{
    /// <summary>
    /// Value serializer that provides serialization for enumeration types.
    /// </summary>
    [GenericValueSerializer]
    public static class EnumSerializer 
    {
        [ValueSerializer.SupportsType]
        public static bool SupportsType(Type type)
            => type.IsEnum;
        
        /// <summary>
        /// Writes given enumeration value to given buffer beginning at given offset.
        /// </summary>
        [ValueSerializer.Serialize]
        public static void Serialize<T>(T value, byte[] buffer, int offset) where T : struct, Enum
        {
            var type = typeof(T).GetEnumUnderlyingType();
            var size = (byte)Marshal.SizeOf(type);
            
            // Write size of the enum as type mask.
            Protocol.SmallContentLength.Write(size, buffer, offset);
            
            offset += Protocol.SmallContentLength.Size;
            
            if      (type == typeof(sbyte))  MemoryMapper.WriteSByte((sbyte)(object)value, buffer, offset);
            else if (type == typeof(byte))   MemoryMapper.WriteByte((byte)(object)value, buffer, offset);
            else if (type == typeof(short))  MemoryMapper.WriteShort((short)(object)value, buffer, offset);
            else if (type == typeof(ushort)) MemoryMapper.WriteUshort((ushort)(object)value, buffer, offset);
            else if (type == typeof(int))    MemoryMapper.WriteInt((int)(object)value, buffer, offset);
            else if (type == typeof(uint))   MemoryMapper.WriteUint((uint)(object)value, buffer, offset);
            else if (type == typeof(long))   MemoryMapper.WriteLong((long)(object)value, buffer, offset);
            else                             MemoryMapper.WriteUlong((ulong)(object)value, buffer, offset);
        }
            
        /// <summary>
        /// Reads next n-bytes from given buffer beginning at given offset as enum type
        /// and returns that value to the caller.
        /// </summary>
        [ValueSerializer.Deserialize]
        public static T Deserialize<T>(byte[] buffer, int offset) where T : struct, Enum
        {
            var type = typeof(T);
            
            // Read size of the enum as type mask.
            offset += Protocol.SmallContentLength.Size;
            
            if (type == typeof(sbyte))  return (T)(object)MemoryMapper.ReadSByte(buffer, offset);
            if (type == typeof(byte))   return (T)(object)MemoryMapper.ReadByte(buffer, offset);
            if (type == typeof(short))  return (T)(object)MemoryMapper.ReadShort(buffer, offset);
            if (type == typeof(ushort)) return (T)(object)MemoryMapper.ReadUshort(buffer, offset);
            if (type == typeof(int))    return (T)(object)MemoryMapper.ReadInt(buffer, offset);
            if (type == typeof(uint))   return (T)(object)MemoryMapper.ReadUint(buffer, offset);
            if (type == typeof(long))   return (T)(object)MemoryMapper.ReadLong(buffer, offset);
            
            return (T)(object)MemoryMapper.ReadUlong(buffer, offset);
        }

        /// <summary>
        /// Returns size of uint32, should always be 4-bytes.
        /// </summary>
        [ValueSerializer.GetSizeFromBuffer]
        public static ushort GetSizeFromBuffer(byte[] buffer, int offset)
            => (ushort)(Protocol.SmallContentLength.Read(buffer, offset) + Protocol.SmallContentLength.Size);

        /// <summary>
        /// Returns size of enumeration value, size can vary.
        /// </summary>
        [ValueSerializer.GetSizeFromValue]
        public static ushort GetSizeFromValue<T>(T value) where T : struct, Enum
            => (ushort)(Protocol.SmallContentLength.Size + Marshal.SizeOf(typeof(T).GetEnumUnderlyingType()));
    }
}