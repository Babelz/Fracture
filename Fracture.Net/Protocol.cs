using System;
using System.Runtime.CompilerServices;
using Fracture.Common.Memory;

namespace Fracture.Net
{
    /// <summary>
    /// Static utility class containing protocol specific constants and functions.
    /// </summary>
    public static class Protocol
    {
        public static class Message
        {
            #region Constants
            /// <summary>
            /// Size of the whole message.
            /// </summary>
            public static class ContentSize
            {
                #region Constants
                public const ushort Size = sizeof(ushort);
                #endregion
                                    
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static void Write(ushort value, byte[] buffer, int offset)
                    => ByteUtils.WriteUshort(value, buffer, offset);
                    
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static ushort Read(byte[] buffer, int offset)
                    => ByteUtils.ReadUshort(buffer, offset);
            }
            
            /// <summary>
            /// Size of the type id of the message.
            /// </summary>
            public static class TypeId
            {
                #region Constants
                public const ushort Size = sizeof(ushort);
                #endregion
                                    
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static void Write(ushort value, byte[] buffer, int offset)
                    => ByteUtils.WriteUshort(value, buffer, offset);
                    
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static ushort Read(byte[] buffer, int offset)
                    => ByteUtils.ReadUshort(buffer, offset);
            }
            #endregion
            
            public static class Field
            {
                /// <summary>
                /// Type id of the message field.
                /// </summary>
                public static class TypeId
                {
                    #region Constants
                    public const ushort Size = sizeof(ushort);
                    #endregion
                                    
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    public static void Write(ushort value, byte[] buffer, int offset)
                        => ByteUtils.WriteUshort(value, buffer, offset);
                    
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    public static ushort Read(byte[] buffer, int offset)
                        => ByteUtils.ReadUshort(buffer, offset);
                }
                
                /// <summary>
                /// Generic type id of the message field if the field is a collection.
                /// </summary>
                public static class GenericTypeId
                {
                    #region Constants
                    public const ushort Size = sizeof(ushort);
                    #endregion
                    
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    public static void Write(ushort value, byte[] buffer, int offset)
                        => ByteUtils.WriteUshort(value, buffer, offset);
                    
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    public static ushort Read(byte[] buffer, int offset)
                        => ByteUtils.ReadUshort(buffer, offset);
                }
                
                /// <summary>
                /// Length of the field if the field is dynamic.
                /// </summary>
                public static class DynamicTypeLength
                {
                    #region Constants
                    public const ushort Size = sizeof(ushort);
                    #endregion
                    
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    public static void Write(ushort value, byte[] buffer, int offset)
                        => ByteUtils.WriteUshort(value, buffer, offset);
                    
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    public static ushort Read(byte[] buffer, int offset)
                        => ByteUtils.ReadUshort(buffer, offset);
                }
            }
        }
    }
}