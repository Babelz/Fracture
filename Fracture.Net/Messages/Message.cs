using System;
using System.Runtime.CompilerServices;
using Fracture.Common.Memory;
using Fracture.Common.Memory.Pools;

namespace Fracture.Net.Messages
{
    /// <summary>
    /// Interface for implementing messages. Messages are passed between servers and clients.
    /// </summary>
    public interface IMessage : IClearable
    {
        // Marker interface, nothing to implement.
    }
    
    /// <summary>
    /// Interface for implementing query messages. Query messages keep the same message between requests and responses.
    /// </summary>
    public interface IQueryMessage : IMessage
    {
        #region Properties
        /// <summary>
        /// Gets or sets the query message id. This id is same for the request and response messages.
        /// </summary>
        int QueryId
        {
            get;
            set;
        }
        #endregion
    }

    /// <summary>
    /// Interface for implementing clock messages. Clock messages will keep the same timestamp between requests and response.
    /// </summary>
    public interface IClockMessage : IMessage
    {
        #region Properties
        /// <summary>
        /// Gets the time the request was created. This timespan should be same between request and response messages.
        /// </summary>
        TimeSpan RequestTime
        {
            get;
            set;
        }
        #endregion
    }
    
    /// <summary>
    /// Static utility class containing message related utilities and shared message pool.
    /// </summary>
    public static class Message
    {
        #region Fields
        private static IMessagePool pool;
        #endregion
        
        #region Properties
        private static IMessagePool Pool
        {
            get => pool ??= new MessagePool();
            set => pool = value;
        }
        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Clock<T>(IClockMessage from, Func<T> result) where T : IClockMessage
        {
            var message = result();
            
            message.RequestTime = from.RequestTime;
            
            return message;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Query<T>(IQueryMessage from, Func<T> result) where T : IQueryMessage
        {
            var message = result();
            
            message.QueryId = from.QueryId;
            
            return message;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Take<T>(PoolElementDecoratorDelegate<T> decorator = null) where T : class, IMessage, new()
            => Pool.Take<T>();
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Return(IMessage message)
            => Pool.Return(message);
    }
}