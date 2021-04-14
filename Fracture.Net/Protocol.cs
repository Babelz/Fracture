using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Fracture.Common.Memory;

namespace Fracture.Net
{
    /// <summary>
    /// Generic delegate for wrapping write calls to buffer.
    /// </summary>
    public delegate void ProtocolHeaderWriteDelegate<in T>(T value, byte[] buffer, int offset);
                  
    /// <summary>
    /// Generic delegate for wrapping read calls to buffer.
    /// </summary>
    public delegate T ProtocolHeaderReadDelegate<out T>(byte[] buffer, int offset);
    
    /// <summary>
    /// Class providing generic wrapper for protocol header fields such as message type id and size fields.
    /// </summary>
    public sealed class ProtocolHeader<T>
    {
        #region Fields
        private readonly ProtocolHeaderReadDelegate<T> read;
        private readonly ProtocolHeaderWriteDelegate<T> write;
        #endregion
        
        #region Properties
        /// <summary>
        /// Gets the size of the header inside a message buffer.
        /// </summary>
        public ushort Size
        {
            get;
        }
        #endregion

        public ProtocolHeader(ProtocolHeaderWriteDelegate<T> write, ProtocolHeaderReadDelegate<T> read)
        {
            Size = (ushort)Marshal.SizeOf<T>();
            
            this.write = write ?? throw new ArgumentNullException(nameof(write));
            this.read  = read ?? throw new ArgumentNullException(nameof(read));
        }
        
        public void Write(T value, byte[] buffer, int offset)
            => write(value, buffer, offset);
        
        public T Read(byte[] buffer, int offset)
            => read(buffer, offset);
    }
    
    /// <summary>
    /// Static utility class for creating protocol headers.
    /// </summary>
    public static class ProtocolHeaderFactory
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ProtocolHeader<ushort> CreateUshort()
            => new ProtocolHeader<ushort>(ByteUtils.WriteUshort, ByteUtils.ReadUshort);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ProtocolHeader<uint> CreateUint()
            => new ProtocolHeader<uint>(ByteUtils.WriteUint, ByteUtils.ReadUint);   
    }
    
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
            public static readonly ProtocolHeader<ushort> ContentSize = ProtocolHeaderFactory.CreateUshort();
            
            /// <summary>
            /// Size of the type id of the message.
            /// </summary>
            public static readonly ProtocolHeader<ushort> TypeId = ProtocolHeaderFactory.CreateUshort();
            #endregion
            
            public static class Field
            {
                /// <summary>
                /// Length of the field if the field size can vary.
                /// </summary>
                public static readonly ProtocolHeader<ushort> ContentSize = ProtocolHeaderFactory.CreateUshort();
            }
        }
    }
}