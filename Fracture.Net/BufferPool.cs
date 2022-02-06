using System.Runtime.CompilerServices;
using Fracture.Common.Collections;
using Fracture.Common.Memory.Pools;
using Fracture.Common.Memory.Storages;

namespace Fracture.Net
{
    public static class BufferPool
    {
        #region Static fields
        private static readonly IArrayPool<byte> Buffers = new ConcurrentArrayPool<byte>(
            new BlockArrayPool<byte>(
                new ArrayPool<byte>(() => new LinearStorageObject<byte[]>(new LinearGrowthArray<byte[]>()), 0), 8, ushort.MaxValue)
            );
        #endregion
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] Take(int size) => Buffers.Take(size);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Return(byte[] buffer) => Buffers.Return(buffer);
    }
}