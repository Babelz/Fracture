using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Fracture.Common.Memory;

namespace Fracture.Net.Serialization
{
    /// <summary>
    /// Generic delegate for wrapping write calls to buffer.
    /// </summary>
    public delegate void HeaderWriteDelegate<in T>(T value, byte [] buffer, int offset);

    /// <summary>
    /// Generic delegate for wrapping read calls to buffer.
    /// </summary>
    public delegate T HeaderReadDelegate<out T>(byte [] buffer, int offset);

    /// <summary>
    /// Class providing generic wrapper for protocol labels such as message type id and size fields.
    /// </summary>
    public sealed class Header<T>
    {
        #region Fields
        private readonly HeaderReadDelegate<T>  read;
        private readonly HeaderWriteDelegate<T> write;
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

        public Header(HeaderWriteDelegate<T> write, HeaderReadDelegate<T> read)
        {
            Size = (ushort)Marshal.SizeOf<T>();

            this.write = write ?? throw new ArgumentNullException(nameof(write));
            this.read  = read ?? throw new ArgumentNullException(nameof(read));
        }

        public void Write(T value, byte [] buffer, int offset)
            => write(value, buffer, offset);

        public T Read(byte [] buffer, int offset)
            => read(buffer, offset);
    }

    public static class Header
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Header<byte> Byte()
            => new Header<byte>(MemoryMapper.WriteByte, MemoryMapper.ReadByte);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Header<ushort> Ushort()
            => new Header<ushort>(MemoryMapper.WriteUshort, MemoryMapper.ReadUshort);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Header<uint> Uint()
            => new Header<uint>(MemoryMapper.WriteUint, MemoryMapper.ReadUint);
    }

    /// <summary>
    /// Static utility class containing serialization protocol specific constants and functions.
    ///
    /// Objects are laid out in the byte stream as follows:
    ///     - 0x00, type-id header
    ///     - 0x02, content-length header
    ///     - 0x04, optional null-mask-length header
    ///     - 0x05, optional null-mask-length amount of bytes
    ///
    /// When talking about objects in context of serialization we are talking about structures and classes that consist from multiple values.
    ///
    /// Values are laid out in the byte stream as follows:
    ///     - 0x00, optional type-specialization-id header for generic and structure values
    ///     - 0x02, optional null-mask-length header
    ///     - 0x03, optional null-mask-length amount of bytes
    ///     - 0x04, optional type-data that contains type specific serialization data 
    ///     - 0x05, optional content-length header for values that can vary in size
    ///
    /// When talking about values in context of serialization we are talking about single values such as integers and strings that on their own make little sense. 
    /// 
    /// If a value is null it will not be serialized to the buffer in any way. Instead if the object being serialized has fields that can be null,
    /// specialized null mask bit field will be generated for the object. This is guaranteed to be generated every time if a type contains fields or
    /// properties that can be null.
    ///
    /// The same masking will be applied to collections that can contain null values. 
    /// </summary>
    public static class Protocol
    {
        #region Constant fields
        /// <summary>
        /// Maximum transmission unit size defined for the protocol.
        /// </summary>
        public const ushort Mtu = ushort.MaxValue;
        #endregion

        #region Static fields
        /// <summary>
        /// Header denoting the type id of the object. This type id can denote a single value or structure in the byte stream.
        /// </summary>
        public static readonly Header<ushort> SerializationTypeId = Header.Ushort();

        /// <summary>
        /// Header denoting content size inside the message in bytes. This header appears on all object values after their type id or with fields that can
        /// vary in size. This is used for storing for example collection package size in streams and string lengths.
        /// </summary>
        public static readonly Header<ushort> ContentLength = Header.Ushort();

        /// <summary>
        /// Header denoting collection length in elements. This header appears on all collection type objects.
        /// </summary>
        public static readonly Header<ushort> CollectionLength = Header.Ushort();

        /// <summary>
        /// Header containing small optional serialization data field used by some serialization types to store additional information about the serialized objects.
        /// </summary>
        public static readonly Header<byte> TypeData = Header.Byte();
        #endregion
    }
}