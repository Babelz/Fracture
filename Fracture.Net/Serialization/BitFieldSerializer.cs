using System;
using System.Diagnostics;
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
        private static int BitIndex(int index) => index % BitsInByte;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ByteIndex(int index) => index / BitsInByte;
        
        /// <summary>
        /// Gets bit value at given index.
        /// </summary>
        public bool GetBit(int index)
        {
            var byteIndex = ByteIndex(index);
            var bitIndex  = BitIndex(index);
            
            return (bytes[byteIndex] >> ((BitsInByte - 1) - bitIndex) & 1) == 1;
        }
        
        /// <summary>
        /// Sets bit value at given index.
        /// </summary>
        public void SetBit(int index, bool value)
        {
            var byteIndex = ByteIndex(index);
            var bitIndex  = BitIndex(index);
            
            bytes[byteIndex] = (byte)(bytes[byteIndex] | ((value ? 1 : 0) << (BitsInByte - 1) - bitIndex));
            
            Debug.WriteLine("SET BIT: " + index + " " + value);
            Debug.WriteLine("BYTE INDEX: " + byteIndex);
            Debug.WriteLine("BIT INDEX: " + bitIndex);
            Debug.WriteLine("BYTE VALUE: " + bytes[byteIndex]);
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
            => MemoryMapper.VectorizedCopy(buffer, offset, bytes, 0, bytes.Length);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LengthFromBits(int bits)
        {
            // Determine how big of a bit field we need and instantiate bit field local.
            var moduloBitsInBitField = (bits % 8);

            // Add one additional byte to the field if we have any bits that don't fill all bytes.
            return (bits / 8) + (moduloBitsInBitField != 0 ? 1 : 0);
        }
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
            
            Protocol.NullMaskLength.Write(checked((byte)actual.Length), buffer, offset);
            
            actual.CopyTo(buffer, offset + Protocol.NullMaskLength.Size);
        }

        public object Deserialize(byte[] buffer, int offset)
        {
            var size = Protocol.NullMaskLength.Read(buffer, offset);
            
            var value = new BitField(size);
            
            value.CopyFrom(buffer, offset + Protocol.NullMaskLength.Size);
            
            return value;
        }

        public ushort GetSizeFromBuffer(byte[] buffer, int offset)
            => Protocol.NullMaskLength.Read(buffer, offset);

        public ushort GetSizeFromValue(object value)
            => (ushort)(Protocol.NullMaskLength.Size + ((BitField)value).Length);
    }
}