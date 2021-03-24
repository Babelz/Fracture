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
        #region Fields
        private static readonly int VectorSpan1 = 1;
        private static readonly int VectorSpan2 = 2;
        private static readonly int VectorSpan3 = 3;
        private static readonly int VectorSpan4 = 4;

        private const int LongSpan1 = sizeof(long);
        private const int LongSpan2 = sizeof(long) + sizeof(long);
        private const int LongSpan3 = sizeof(long) + sizeof(long) + sizeof(long);
        private const int IntSpan1  = sizeof(int);
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
        public static void Write<T>(byte[] destination, int destinationIndex, T[] source, int sourceIndex, int count) where T : struct
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
        /// Unsafe read for arrays, see <see cref="Read{T}(T[],int,byte[],int,int)"/>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Read<T>(T[] destination, int destinationIndex, byte[] source, int sourceIndex, int count) where T : struct
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
            => (ushort)((bytes[index]) | (bytes[index + 1] << 8));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadInt(byte[] bytes, int index)   
            => (bytes[index]) | (bytes[index + 1] << 8) | (bytes[index + 2] << 16) | (bytes[index + 3] << 24);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReadUint(byte[] bytes, int index) 
            => (uint)((bytes[index]) | (bytes[index + 1] << 8) | (bytes[index + 2] << 16) | (bytes[index + 3] << 24));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDouble(double value, byte[] bytes, int index)
        {
            Write(value, bytes, index);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteFloat(float value, byte[] bytes, int index)
        {
            Write(value, bytes, index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBool(bool value, byte[] bytes, int index)
        {
            bytes[index] = (byte)(value ? 1 : 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteByte(byte value, byte[] bytes, int index)
        {
            bytes[index] = value;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteSByte(sbyte value, byte[] bytes, int index)
        {
            bytes[index] = (byte)value;
        }

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
        {
            bytes[index]     |= (byte)(value);
            bytes[index + 1] |= (byte)(value >> 8);
        }

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
        {
            bytes[index]     |= (byte)(value);
            bytes[index + 1] |= (byte)(value >> 8);
            bytes[index + 2] |= (byte)(value >> 16);
            bytes[index + 3] |= (byte)(value >> 24);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void VectorizedCopy(byte[] source, int sourceIndex, byte[] destination, int destinationIndex, int count)
        {
            if (Vector.IsHardwareAccelerated)
            {
                if (count > 512 + 64)
                {
                    // In-built copy faster for large arrays.
                    Array.Copy(source, sourceIndex, destination, destinationIndex, count);
                    
                    return;
                }
                
                if (source == null) 
                    throw new ArgumentNullException(nameof(source));
                
                if (destination == null) 
                    throw new ArgumentNullException(nameof(destination));

                if (count < 0 || sourceIndex < 0 || destinationIndex < 0) 
                    throw new ArgumentOutOfRangeException(nameof(count));
                
                if (sourceIndex + count > source.Length) 
                    throw new ArgumentException(nameof(source));
                
                if (destinationIndex + count > destination.Length) 
                    throw new ArgumentException(nameof(destination));
                
                if (count == 0) 
                    return;

                while (count >= VectorSpan4)
                {
                    new Vector<byte>(source, sourceIndex).CopyTo(destination, destinationIndex);
                    new Vector<byte>(source, sourceIndex + VectorSpan1).CopyTo(destination, destinationIndex + VectorSpan1);
                    new Vector<byte>(source, sourceIndex + VectorSpan2).CopyTo(destination, destinationIndex + VectorSpan2);
                    new Vector<byte>(source, sourceIndex + VectorSpan3).CopyTo(destination, destinationIndex + VectorSpan3);
                    
                    if (count == VectorSpan4) 
                        return;
                    
                    count            -= VectorSpan4;
                    sourceIndex      += VectorSpan4;
                    destinationIndex += VectorSpan4;
                }
                
                if (count >= VectorSpan2)
                {
                    new Vector<byte>(source, sourceIndex).CopyTo(destination, destinationIndex);
                    new Vector<byte>(source, sourceIndex + VectorSpan1).CopyTo(destination, destinationIndex + VectorSpan1);
                    
                    if (count == VectorSpan2) 
                        return;
                    
                    count            -= VectorSpan2;
                    sourceIndex      += VectorSpan2;
                    destinationIndex += VectorSpan2;
                }
                
                if (count >= VectorSpan1)
                {
                    new Vector<byte>(source, sourceIndex).CopyTo(destination, destinationIndex);
                    
                    if (count == VectorSpan1) 
                        return;
                    
                    count            -= VectorSpan1;
                    sourceIndex      += VectorSpan1;
                    destinationIndex += VectorSpan1;
                }
                
                if (count > 0)
                {
                    fixed (byte* srcOrigin = source)
                    fixed (byte* dstOrigin = destination)
                    {
                        var pSrc = srcOrigin + sourceIndex;
                        var dSrc = dstOrigin + destinationIndex;

                        if (count >= LongSpan1)
                        {
                            var lpSrc = (long*)pSrc;
                            var ldSrc = (long*)dSrc;

                            if (count < LongSpan2)
                            {
                                count  -= LongSpan1;
                                pSrc   += LongSpan1;
                                dSrc   += LongSpan1;
                                *ldSrc = *lpSrc;
                            }
                            else if (count < LongSpan3)
                            {
                                count -= LongSpan2;
                                pSrc  += LongSpan2;
                                dSrc  += LongSpan2;
                                
                                *ldSrc       = *lpSrc;
                                *(ldSrc + 1) = *(lpSrc + 1);
                            }
                            else
                            {
                                count -= LongSpan3;
                                pSrc  += LongSpan3;
                                dSrc  += LongSpan3;
                                
                                *ldSrc       = *lpSrc;
                                *(ldSrc + 1) = *(lpSrc + 1);
                                *(ldSrc + 2) = *(lpSrc + 2);
                            }
                        }
                        if (count >= IntSpan1)
                        {
                            var ipSrc = (int*)pSrc;
                            var idSrc = (int*)dSrc;
                            
                            count -= IntSpan1;
                            pSrc  += IntSpan1;
                            dSrc  += IntSpan1;
                            
                            *idSrc = *ipSrc;
                        }
                        while (count > 0)
                        {
                            count--;
                            
                            *dSrc = *pSrc;
                            
                            dSrc += 1;
                            pSrc += 1;
                        }
                    }
                }
                
                return;
            }

            Array.Copy(source, sourceIndex, destination, destinationIndex, count);
        }
    }
}