using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Fracture.Common.Memory;

namespace Fracture.Net.Serialization
{
    /// <summary>
    /// Structure that represents variable length bit field.
    /// </summary>
    public readonly struct BitField
    {
        #region Constant fields
        private const int BitsInByte = 8;
        #endregion
        
        #region Fields
        private readonly byte[] bytes; 
        #endregion

        #region Properties
        /// <summary>
        /// Gets the length of the bit field in bytes.
        /// </summary>
        public int Length => bytes.Length;
        #endregion

        /// <summary>
        /// Creates new instance of <see cref="BitField"/> with given length.
        /// </summary>
        /// <param name="length">how many bytes does the bit field consist of</param>
        public BitField(int length)
            => bytes = new byte[length];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int BitIndex(int index) => index % sizeof(byte);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ByteIndex(int index) => index / sizeof(byte);
        
        /// <summary>
        /// Gets bit value at given index.
        /// </summary>
        public bool GetBit(int index)
        {
            var byteIndex = ByteIndex(index);
            var bitIndex  = BitIndex(index);
            
            return (bytes[byteIndex] & (1 << BitsInByte - bitIndex)) == 1;
        }
        
        /// <summary>
        /// Sets bit value at given index.
        /// </summary>
        public void SetBit(int index, bool value)
        {
            var byteIndex = ByteIndex(index);
            var bitIndex  = BitIndex(index);
            
            bytes[byteIndex] = (byte)(bytes[byteIndex] | (1 << BitsInByte - bitIndex));
        }
        
        /// <summary>
        /// Gets byte at given index.
        /// </summary>
        public byte GetByteAtIndex(int index)
            => bytes[index];
        
        /// <summary>
        /// Copies bit field bytes to given buffer at given offset.
        /// </summary>
        public void CopyTo(byte[] buffer, int offset)
            => MemoryMapper.VectorizedCopy(bytes, 0, buffer, offset, bytes.Length);
        
        /// <summary>
        /// Copies buffer values to bit field beginning from given offset.
        /// </summary>
        public void CopyFrom(byte[] buffer, int offset)
            => MemoryMapper.VectorizedCopy(bytes, 0, buffer, offset, bytes.Length);
    }
    
    /// <summary>
    /// Value serializer that provides serialization for <see cref="BitField"/>.
    /// </summary>
    public sealed class BitFieldSerializer : IValueSerializer
    {
        public BitFieldSerializer()
        {
        }
        
        public bool SupportsType(Type type)
            => type == typeof(BitField);

        public void Serialize(object value, byte[] buffer, int offset)
        {
            var actual = (BitField)value;
            
            Protocol.Value.NullBitFieldSize.Write(checked((byte)buffer.Length), buffer, offset);
            
            actual.CopyTo(buffer, offset + Protocol.Value.NullBitFieldSize.Size);
        }

        public object Deserialize(byte[] buffer, int offset)
        {
            var size = Protocol.Value.NullBitFieldSize.Read(buffer, offset);
            
            var value = new BitField(size);
            
            value.CopyFrom(buffer, offset + Protocol.Value.NullBitFieldSize.Size);
            
            return value;
        }

        public ushort GetSizeFromBuffer(byte[] buffer, int offset)
            => Protocol.Value.NullBitFieldSize.Read(buffer, offset);

        public ushort GetSizeFromValue(object value)
            => (ushort)(Protocol.Value.NullBitFieldSize.Size + ((BitField)value).Length);
    }
}