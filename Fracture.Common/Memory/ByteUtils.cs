using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Fracture.Common.Memory
{
    /// <summary>
    /// Unsafe utility class for writing and reading primitive values from byte arrays.
    /// </summary>
    public static unsafe class ByteUtils
    {
        #region Constant fields
        // After what amount of bytes Array.Copy will be faster than using SIMD.
        private const int VectorCopyThreshold = 576;
        #endregion
        
        #region Static fields
        private static readonly int VectorSpan = Vector<byte>.Count;
        #endregion
        
        /// <summary>
        /// Generic unsafe read function that reads sizeof(T) of bytes from given index. Functions pretty much
        /// as a re-interpret cast.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Read<T>(byte[] bytes, int index) where T : struct
        {
            fixed (byte* ptr = &bytes[index]) return Marshal.PtrToStructure<T>(new IntPtr(ptr));
        }
        
        /// <summary>
        /// Generic unsafe write function that writes sizeof(T) of bytes beginning from given index. 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write<T>(T value, byte[] bytes, int index) where T : struct
        {
            fixed (byte* ptr = &bytes[index]) Marshal.StructureToPtr(value, new IntPtr(ptr), false);
        }
        
        /// <summary>
        /// Unsafe write for arrays, see <see cref="Write{T}(T,byte[],int)"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteArray<T>(byte[] destination, int destinationIndex, T[] source, int sourceIndex, int count) where T : struct
        {
            var size         = Marshal.SizeOf(typeof(T));
            var requiredSize = size * count;

            if (destination.Length < requiredSize) throw new InvalidOperationException("dest < required");

            fixed (byte* dstPtr = &destination[0])
            {
                var ptr = new IntPtr(dstPtr);

                for (var i = sourceIndex; i < sourceIndex + count; i++)
                {
                    Marshal.StructureToPtr(source[i], ptr, false);

                    ptr += size;
                }
            }
        }
        /// <summary>
        /// Unsafe read for arrays, see <see cref="ReadArray{T}"/>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadArray<T>(T[] destination, int destinationIndex, byte[] source, int sourceIndex, int count) where T : struct
        {
            var size         = Marshal.SizeOf(typeof(T));
            var requiredSize = count / size;

            if (destination.Length < requiredSize) throw new InvalidOperationException("dest < required");

            fixed (byte* srcPtr = &source[0])
            {
                var ptr = new IntPtr(srcPtr);

                for (var i = sourceIndex; i < sourceIndex + count; i++)
                {
                    destination[destinationIndex++] = Marshal.PtrToStructure<T>(ptr);

                    ptr += size;
                }
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ReadDouble(byte[] bytes, int index) 
            => Read<double>(bytes, index);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ReadFloat(byte[] bytes, int index)   
            => Read<float>(bytes, index);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ReadBool(byte[] bytes, int index) 
            => bytes[index] != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ReadByte(byte[] bytes, int index)   
            => bytes[index];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static sbyte ReadSByte(byte[] bytes, int index) 
            => (sbyte)(bytes[index]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char ReadChar(byte[] bytes, int index) 
            => (char)((bytes[index]) | (bytes[index + 1] << 8));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short ReadShort(byte[] bytes, int index)   
            => (short)((bytes[index]) | (bytes[index + 1] << 8));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ReadUshort(byte[] bytes, int index) 
            => (ushort)ReadShort(bytes, index);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadInt(byte[] bytes, int index)   
            => (bytes[index]) | (bytes[index + 1] << 8) | (bytes[index + 2] << 16) | (bytes[index + 3] << 24);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReadUint(byte[] bytes, int index) 
            => (uint)ReadInt(bytes, index);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ReadLong(byte[] bytes, int index)   
            => Read<long>(bytes, index);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ReadUlong(byte[] bytes, int index) 
            => (ulong)ReadLong(bytes, index);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDouble(double value, byte[] bytes, int index)
            => Write(value, bytes, index);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteFloat(float value, byte[] bytes, int index)
            => Write(value, bytes, index);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBool(bool value, byte[] bytes, int index)
            => bytes[index] = (byte)(value ? 1 : 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteByte(byte value, byte[] bytes, int index)
            => bytes[index] = value;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteSByte(sbyte value, byte[] bytes, int index)
            => WriteByte((byte)value, bytes, index);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteChar(char value, byte[] bytes, int index)
        {
            bytes[index]     |= (byte)(value);
            bytes[index + 1] |= (byte)(value >> 8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteShort(short value, byte[] bytes, int index)
        {
            bytes[index]     |= (byte)(value);
            bytes[index + 1] |= (byte)(value >> 8);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUshort(ushort value, byte[] bytes, int index)
            => WriteShort((short)value, bytes, index);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt(int value, byte[] bytes, int index)
        {
            bytes[index]     |= (byte)(value);
            bytes[index + 1] |= (byte)(value >> 8);
            bytes[index + 2] |= (byte)(value >> 16);
            bytes[index + 3] |= (byte)(value >> 24);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUint(uint value, byte[] bytes, int index)
            => WriteInt((int)value, bytes, index);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteLong(long value, byte[] bytes, int index)
        {
            bytes[index]     |= (byte)(value);
            bytes[index + 1] |= (byte)(value >> 8);
            bytes[index + 2] |= (byte)(value >> 16);
            bytes[index + 3] |= (byte)(value >> 24);
            bytes[index + 4] |= (byte)(value >> 32);
            bytes[index + 5] |= (byte)(value >> 40);
            bytes[index + 6] |= (byte)(value >> 48);
            bytes[index + 7] |= (byte)(value >> 56);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUlong(ulong value, byte[] bytes, int index)
            => WriteLong((long)value, bytes, index);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void VectorizedCopy(byte[] source, int sourceIndex, byte[] destination, int destinationIndex, int count)
        {
            if (Vector.IsHardwareAccelerated || count < Vector<byte>.Count)
            {
                if (count > VectorCopyThreshold)
                {
                    // In-built copy faster for large arrays.
                    Array.Copy(source, sourceIndex, destination, destinationIndex, count);
                    
                    return;
                }

                while (count >= Vector<byte>.Count)
                {
                    new Vector<byte>(source, sourceIndex).CopyTo(destination, destinationIndex);
                    
                    count            -= VectorSpan;
                    sourceIndex      += VectorSpan;
                    destinationIndex += VectorSpan;
                }
                
                for (var i = 0; i < count; i++)
                    destination[destinationIndex++] = source[sourceIndex++];
                
                return;
            }

            Array.Copy(source, sourceIndex, destination, destinationIndex, count);
        }
    }
}