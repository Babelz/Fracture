using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Castle.Core.Internal;
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
        #region Constant fields
        public const int DefaultMessagePoolSize = 32;
        #endregion

        #region Static fields
        private static readonly Dictionary<Type, IPool<IMessage>> Pools = new Dictionary<Type, IPool<IMessage>>();

        private static int messagePoolSize;
        #endregion

        static Message()
            => messagePoolSize = DefaultMessagePoolSize;

        public virtual void Clear()
        {
        }

        public override string ToString()
            => JsonConvert.SerializeObject(this);

        /// <summary>
        /// Configures the internal pooling for all message types to use the defined bucket sizing. The messages will be pre-allocated. This method should be called before using
        /// the actual pool.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ConfigurePooling(int bucketSize)
        {
            if (bucketSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(bucketSize));

            messagePoolSize = bucketSize;
        }

        /// <summary>
        /// Configures pooling for specified message type. This method should be called before using the actual pool.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ConfigurePooling<T>(int bucketSize) where T : class, IMessage, new()
        {
            if (bucketSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(bucketSize));

            // ReSharper disable once InconsistentlySynchronizedField - should not be called after application configuration.
            Pools.Add(typeof(T), new CleanPool<IMessage>(
                          new LinearPool<IMessage>(new LinearStorageObject<IMessage>(new LinearGrowthArray<IMessage>(bucketSize)), () => new T(), bucketSize))
            );
        }

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

            lock (Pools)
            {
                // Automatically 
                if (!Pools.TryGetValue(type, out var pool))
                {
                    pool = new CleanPool<IMessage>(
                        new LinearPool<IMessage>(new LinearStorageObject<IMessage>(new LinearGrowthArray<IMessage>(messagePoolSize)), () => new T(), messagePoolSize)
                    );

                    Pools.Add(type, pool);
                }

                var message = (T)pool.Take();

                decorator?.Invoke(message);

                return message;
            }
        }

        /// <summary>
        /// Returns boolean declaring whether the given messages type is pooled message type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPooled(in IMessage message)
        {
            lock (Pools)
            {
                return Pools.ContainsKey(message.GetType());
            }
        }

        /// <summary>
        /// Attempts to return given message back to the pool. Has no effect if the message type is not a pooled message type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Release(in IMessage message)
        {
            lock (Pools)
            {
                if (IsPooled(message))
                    Pools[message.GetType()].Return(message);
            }
        }
    }
}