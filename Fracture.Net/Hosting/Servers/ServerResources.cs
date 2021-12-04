using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Fracture.Common.Collections;
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
        
        public static class EventArgs
        {
            public static class ServerMessage 
            {
                #region Fields
                private static readonly IPool<ServerMessageEventArgs> Pool;
                #endregion

                static ServerMessage()
                    => Pool = new CleanPool<ServerMessageEventArgs>(
                           new Pool<ServerMessageEventArgs>(new LinearStorageObject<ServerMessageEventArgs>(new LinearGrowthArray<ServerMessageEventArgs>()))
                       );
                
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static ServerMessageEventArgs Take(PoolElementDecoratorDelegate<ServerMessageEventArgs> decorator = null) 
                    => Pool.Take(decorator);
                
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static void Return(ServerMessageEventArgs args)
                    => Pool.Return(args);
            }
            public static class PeerMessage 
            {
                #region Fields
                private static readonly IPool<PeerMessageEventArgs> Pool;
                #endregion

                static PeerMessage()
                    => Pool = new CleanPool<PeerMessageEventArgs>(
                           new Pool<PeerMessageEventArgs>(new LinearStorageObject<PeerMessageEventArgs>(new LinearGrowthArray<PeerMessageEventArgs>()))
                       );
                
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static PeerMessageEventArgs Take(PoolElementDecoratorDelegate<PeerMessageEventArgs> decorator = null) 
                    => Pool.Take(decorator);
                
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static void Return(PeerMessageEventArgs args)
                    => Pool.Return(args);
            }
            public static class PeerReset 
            {
                #region Fields
                private static readonly IPool<PeerResetEventArgs> Pool;
                #endregion

                static PeerReset()
                    => Pool = new CleanPool<PeerResetEventArgs>(
                           new Pool<PeerResetEventArgs>(new LinearStorageObject<PeerResetEventArgs>(new LinearGrowthArray<PeerResetEventArgs>()))
                       );
                
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static PeerResetEventArgs Take(PoolElementDecoratorDelegate<PeerResetEventArgs> decorator = null) 
                    => Pool.Take(decorator);
                
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static void Return(PeerResetEventArgs args)
                    => Pool.Return(args);
            }
            public static class PeerJoin
            {
                #region Fields
                private static readonly IPool<PeerJoinEventArgs> Pool;
                #endregion

                static PeerJoin()
                    => Pool = new CleanPool<PeerJoinEventArgs>(
                           new Pool<PeerJoinEventArgs>(new LinearStorageObject<PeerJoinEventArgs>(new LinearGrowthArray<PeerJoinEventArgs>()))
                        );
                
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static PeerJoinEventArgs Take(PoolElementDecoratorDelegate<PeerJoinEventArgs> decorator = null) 
                    => Pool.Take(decorator);
                
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static void Return(PeerJoinEventArgs args)
                    => Pool.Return(args);
            }
        }
    }
}