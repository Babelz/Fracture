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
            var size = (byte)Marshal.SizeOf(typeof(T).GetEnumUnderlyingType());
            
            // Write size of the enum as type mask.
            Protocol.SmallContentLength.Write(size, buffer, offset);
            
            offset += Protocol.SmallContentLength.Size;
            
            MemoryMapper.Write(value, buffer, offset);
        }
            
        /// <summary>
        /// Reads next n-bytes from given buffer beginning at given offset as enum type
        /// and returns that value to the caller.
        /// </summary>
        [ValueSerializer.Deserialize]
        public static T Deserialize<T>(byte[] buffer, int offset) where T : struct, Enum
        {
            // Read size of the enum as type mask.
            offset += Protocol.SmallContentLength.Size;
            
            return MemoryMapper.Read<T>(buffer, offset);
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
        public static ushort GetSizeFromValue<T>(T value) where T : Enum
            => (ushort)(Protocol.SmallContentLength.Size + Marshal.SizeOf(typeof(T).GetEnumUnderlyingType()));
    }
}