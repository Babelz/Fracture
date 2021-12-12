using System.Runtime.CompilerServices;
using Fracture.Common.Collections;
using Fracture.Common.Memory.Pools;
using Fracture.Common.Memory.Storages;
using Fracture.Net.Messages;

namespace Fracture.Net.Hosting
{
    /// TODO: no initial allocations is done for the object pools, this might negatively affect the performance so monitor this and checkout if
    ///       pre-allocation can fix this.
    public static class ApplicationResources
    {
        public static class Message
        {
            #region Fields
            private static readonly IMessagePool Pool = new MessagePool();
            #endregion
        
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static T Take<T>(PoolElementDecoratorDelegate<T> decorator = null) where T : class, IMessage, new()
                => Pool.Take<T>();
        
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Return(IMessage message)
                => Pool.Return(message);
        }
        
        public static class Notification
        {
            #region Fields
            private static readonly IPool<Messaging.Notification> Pool = new CleanPool<Messaging.Notification>(
                new Pool<Messaging.Notification>(new LinearStorageObject<Messaging.Notification>(new LinearGrowthArray<Messaging.Notification>(256)))
            );
            #endregion
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Messaging.Notification Take() => Pool.Take();
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Return(Messaging.Notification notification) => Pool.Return(notification);
        }
        
        public static class Response
        {
            #region Fields
            private static readonly IPool<Messaging.Response> Pool = new CleanPool<Messaging.Response>(
                new Pool<Messaging.Response>(new LinearStorageObject<Messaging.Response>(new LinearGrowthArray<Messaging.Response>(256)))
            );
            #endregion
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Messaging.Response Take() => Pool.Take();
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Return(Messaging.Response response) => Pool.Return(response);
        }
        
        public static class Request
        {
            #region Fields
            private static readonly IPool<Messaging.Request> Pool = new CleanPool<Messaging.Request>(
                new Pool<Messaging.Request>(new LinearStorageObject<Messaging.Request>(new LinearGrowthArray<Messaging.Request>(256)))
            );
            #endregion
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Messaging.Request Take() => Pool.Take();
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Return(Messaging.Request request) => Pool.Return(request);
        }
    }
}