using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Fracture.Common.Memory.Pools;
using Fracture.Common.Memory.Storages;

namespace Fracture.Net.Hosting.Servers
{
    public static class ServerResources
    {
        public static class BlockBuffer
        {
            #region Fields
            private static readonly IArrayPool<byte> Pool = new ConcurrentArrayPool<byte>(
                new BlockArrayPool<byte>(
                    new ArrayPool<byte>(() => new ListStorageObject<byte[]>(new List<byte[]>()), 0), 64, ushort.MaxValue)
            );
            #endregion
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static byte[] Take(int size) => Pool.Take(size);
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Return(byte[] buffer) => Pool.Return(buffer);
        }
    }
}