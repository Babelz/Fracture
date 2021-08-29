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
        #region Static fields
        private static readonly Dictionary<Type, GetSizeFromValueDelegate<object>> GetSizeFromValueDelegates = new Dictionary<Type, GetSizeFromValueDelegate<object>>()
        {
            // These usually just translate to constant values at runtime so the boxing is bit stupid. This could be replaced with constant values but if the 
            // schematics for serialization changes this would break likely.
            { typeof(sbyte),  (value) => SbyteSerializer.GetSizeFromValue((sbyte)value)   },
            { typeof(byte),   (value) => ByteSerializer.GetSizeFromValue((byte)value)     },
            { typeof(short),  (value) => ShortSerializer.GetSizeFromValue((short)value)   },
            { typeof(ushort), (value) => UshortSerializer.GetSizeFromValue((ushort)value) },
            { typeof(int),    (value) => IntSerializer.GetSizeFromValue((int)value)       },
            { typeof(uint),   (value) => UintSerializer.GetSizeFromValue((uint)value)     },
            { typeof(long),   (value) => LongSerializer.GetSizeFromValue((long)value)     },
            { typeof(ulong),  (value) => UlongSerializer.GetSizeFromValue((ulong)value)   }
        };
        
        private static readonly Dictionary<Type, SerializeDelegate<object>> SerializeDelegates = new Dictionary<Type, SerializeDelegate<object>>()
        {
            { typeof(sbyte),  (value, buffer, offset) => SbyteSerializer.Serialize((sbyte)value, buffer, offset)   },
            { typeof(byte),   (value, buffer, offset) => ByteSerializer.Serialize((byte)value, buffer, offset)     },
            { typeof(short),  (value, buffer, offset) => ShortSerializer.Serialize((short)value, buffer, offset)   },
            { typeof(ushort), (value, buffer, offset) => UshortSerializer.Serialize((ushort)value, buffer, offset) },
            { typeof(int),    (value, buffer, offset) => IntSerializer.Serialize((int)value, buffer, offset)       },
            { typeof(uint),   (value, buffer, offset) => UintSerializer.Serialize((uint)value, buffer, offset)     },
            { typeof(long),   (value, buffer, offset) => LongSerializer.Serialize((long)value, buffer, offset)     },
            { typeof(ulong),  (value, buffer, offset) => UlongSerializer.Serialize((ulong)value, buffer, offset)   }
        };
        
        private static readonly Dictionary<Type, DeserializeDelegate<object>> DeserializeDelegates = new Dictionary<Type, DeserializeDelegate<object>>()
        {
            { typeof(sbyte),  (buffer, offset) => SbyteSerializer.Deserialize(buffer, offset)  },
            { typeof(byte),   (buffer, offset) => ByteSerializer.Deserialize(buffer, offset)   },
            { typeof(short),  (buffer, offset) => ShortSerializer.Deserialize(buffer, offset)  },
            { typeof(ushort), (buffer, offset) => UshortSerializer.Deserialize(buffer, offset) },
            { typeof(int),    (buffer, offset) => IntSerializer.Deserialize(buffer, offset)    },
            { typeof(uint),   (buffer, offset) => UintSerializer.Deserialize(buffer, offset)   },
            { typeof(long),   (buffer, offset) => LongSerializer.Deserialize(buffer, offset)   },
            { typeof(ulong),  (buffer, offset) => UlongSerializer.Deserialize(buffer, offset)  }
        };
        #endregion
        
        [ValueSerializer.SupportsType]
        public static bool SupportsType(Type type)
            => type.IsEnum;
        
        /// <summary>
        /// Writes given enumeration value to given buffer beginning at given offset.
        /// </summary>
        [ValueSerializer.Serialize]
        public static void Serialize<T>(T value, byte[] buffer, int offset) where T : struct, Enum
        {
            var underlyingType = typeof(T).GetEnumUnderlyingType();
            
            // Write size of the enum as type mask.
            Protocol.TypeData.Write((byte)GetSizeFromValueDelegates[underlyingType](value), buffer, offset);
            
            offset += Protocol.TypeData.Size;
            
            SerializeDelegates[underlyingType](value, buffer, offset);
        }
            
        /// <summary>
        /// Reads next n-bytes from given buffer beginning at given offset as enum type
        /// and returns that value to the caller.
        /// </summary>
        [ValueSerializer.Deserialize]
        public static T Deserialize<T>(byte[] buffer, int offset) where T : struct, Enum
        {
            // Read size of the enum as type mask.
            offset += Protocol.TypeData.Size;
            
            return (T)DeserializeDelegates[typeof(T).GetEnumUnderlyingType()](buffer, offset);
        }

        /// <summary>
        /// Returns size of enum, can vary between 1 to 4-bytes plus the added small content length size.
        /// </summary>
        [ValueSerializer.GetSizeFromBuffer]
        public static ushort GetSizeFromBuffer(byte[] buffer, int offset)
            => (ushort)(Protocol.TypeData.Size + Protocol.TypeData.Read(buffer, offset));

        /// <summary>
        /// Returns size of enumeration value, size can vary between 1 to 4-bytes plus the added small content length size.
        /// </summary>
        [ValueSerializer.GetSizeFromValue]
        public static ushort GetSizeFromValue<T>(T value) where T : struct, Enum
            => (ushort)(Protocol.TypeData.Size + GetSizeFromValueDelegates[typeof(T).GetEnumUnderlyingType()](value));
    }
}