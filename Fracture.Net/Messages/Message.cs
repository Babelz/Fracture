using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Fracture.Common.Collections;
using Fracture.Common.Memory;
using Fracture.Common.Memory.Pools;
using Fracture.Common.Memory.Storages;
using Fracture.Net.Serialization;
using Fracture.Net.Serialization.Generation;
using Newtonsoft.Json;

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
    
    public delegate void ObjectSchemaMapDelegate(ObjectSerializationMapper schema);

    /// <summary>
    /// Abstract base class for creating messages that also provides message pooling and message related utilities.
    /// </summary>
    public abstract class Message : IMessage
    {
        #region Static fields
        private static readonly ConcurrentDictionary<Type, IPool<IMessage>> Pools = new ConcurrentDictionary<Type, IPool<IMessage>>();
        #endregion

        protected Message()
        {
        }
        
        public virtual void Clear()
        {
        }

        public override string ToString()
            => JsonConvert.SerializeObject(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Clock<T>(in IClockMessage from, Func<T> result) where T : IClockMessage
        {
            var message = result();
            
            message.RequestTime = from.RequestTime;
            
            return message;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Query<T>(in IQueryMessage from, Func<T> result) where T : IQueryMessage
        {
            var message = result();
            
            message.QueryId = from.QueryId;
            
            return message;
        }

        /// <summary>
        /// Returns message of preferred type to the caller from pool. Remember to return this message back to the pool using <see cref="Release"/> method.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Create<T>(PoolElementDecoratorDelegate<T> decorator = null) where T : class, IMessage, new()
        {
            var type = typeof(T);
            
            if (!StructSerializer.SupportsType(type))
                throw new InvalidOperationException($"no schema is registered for message type {type.Name}");
            
            if (!Pools.TryGetValue(type, out var pool))
            {
                pool = new CleanPool<IMessage>(
                    new DelegatePool<IMessage>(new LinearStorageObject<IMessage>(new LinearGrowthArray<IMessage>(8)), 
                                               () => new T(), 8)
                );
                
                Pools.TryAdd(type, pool);
            }
                
            var message = (T)pool.Take();
            
            decorator?.Invoke(message);
            
            return message;
        }

        /// <summary>
        /// Returns boolean declaring whether the given messages type is pooled message type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPooled(in IMessage message)
            => Pools.ContainsKey(message.GetType());
        
        /// <summary>
        /// Attempts to return given message back to the pool. Has no effect if the message type is not a pooled message type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Release(in IMessage message)
        {
            if (IsPooled(message))
                Pools[message.GetType()].Return(message);
        }
    }
    
    /// <summary>
    /// Utility class for defining messaging schemas. Works only as wrapper around serialization library. For any more fine tuned schema mapping use the
    /// serialization type mappers.
    /// </summary>
    public static class MessageSchema
    {
        /// <summary>
        /// Defines message inside the schema and maps it for usage.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ForMessage<T>(ObjectSchemaMapDelegate map) where T : IMessage
        {
            var mapper = ObjectSerializationMapper.ForType<T>();
            
            map(mapper);
            
            var mapping = mapper.Map();
            
            StructSerializer.Map(mapping);
        }
        
        /// <summary>
        /// Defines structure that can be found inside the messages and maps it for usage.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ForStruct<T>(ObjectSchemaMapDelegate map) 
        {
            var mapper = ObjectSerializationMapper.ForType<T>();
            
            map(mapper);
            
            var mapping = mapper.Map();
            
            StructSerializer.Map(mapping);
        }
    }
}