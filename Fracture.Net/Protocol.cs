using System;
using System.ComponentModel.Design;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Fracture.Common.Memory;

namespace Fracture.Net
{
    /// <summary>
    /// Generic delegate for wrapping write calls to buffer.
    /// </summary>
    public delegate void ProtocolLabelWriteDelegate<in T>(T value, byte[] buffer, int offset);
                  
    /// <summary>
    /// Generic delegate for wrapping read calls to buffer.
    /// </summary>
    public delegate T ProtocolLabelReadDelegate<out T>(byte[] buffer, int offset);
    
    /// <summary>
    /// Class providing generic wrapper for protocol labels such as message type id and size fields.
    /// </summary>
    public sealed class ProtocolLabel<T>
    {
        #region Fields
        private readonly ProtocolLabelReadDelegate<T> read;
        private readonly ProtocolLabelWriteDelegate<T> write;
        #endregion
        
        #region Properties
        /// <summary>
        /// Gets the size of the label inside a message buffer.
        /// </summary>
        public ushort Size
        {
            get;
        }
        #endregion

        public ProtocolLabel(ProtocolLabelWriteDelegate<T> write, ProtocolLabelReadDelegate<T> read)
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
    
    public static class ProtocolLabel
    {        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ProtocolLabel<byte> Byte()
            => new ProtocolLabel<byte>(MemoryMapper.WriteByte, MemoryMapper.ReadByte);  
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ProtocolLabel<ushort> Ushort()
            => new ProtocolLabel<ushort>(MemoryMapper.WriteUshort, MemoryMapper.ReadUshort);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ProtocolLabel<uint> Uint()
            => new ProtocolLabel<uint>(MemoryMapper.WriteUint, MemoryMapper.ReadUint);  
    }

    /// <summary>
    /// Static utility class containing serialization protocol specific constants and functions.
    /// </summary>
    public static class Protocol
    {
        /// <summary>
        /// Static class containing object specific labels.
        /// </summary>
        public static class Object
        {
            #region Static fields
            /// <summary>
            /// Label denoting the type id of the object.
            /// </summary>
            public static readonly ProtocolLabel<ushort> TypeId = ProtocolLabel.Ushort();
            
            /// <summary>
            /// Label denoting the size of the whole header.
            /// </summary>
            public static readonly ProtocolLabel<ushort> ContentSize = ProtocolLabel.Ushort();
            #endregion
        }
        
        /// <summary>
        /// Static class containing value specific protocol labels.
        /// </summary>
        public static class Value
        {
            #region Static fields
            /// <summary>
            /// Label denoting the dynamic content size of the field if the field size is varying.
            /// </summary>
            public static readonly ProtocolLabel<ushort> ContentSize = ProtocolLabel.Ushort();
                
            /// <summary>
            /// Label denoting the type specialization id of the field if the type is specialized run type. 
            /// </summary>
            public static readonly ProtocolLabel<ushort> TypeSpecializationId = ProtocolLabel.Ushort();
            #endregion
        }
    }
}