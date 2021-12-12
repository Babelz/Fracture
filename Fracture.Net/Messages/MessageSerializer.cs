using System;
using System.Collections.Generic;
using Fracture.Common.Collections;
using Fracture.Common.Memory.Pools;
using Fracture.Common.Memory.Storages;
using Fracture.Net.Serialization;

namespace Fracture.Net.Messages
{
    /// <summary>
    /// Interface for implementing message serializers. Providers layer of abstraction for serializing and deserializing with different schemas.
    /// </summary>
    public interface IMessageSerializer
    {
        /// <summary>
        /// Serializes given message to buffer and returns it to the caller.
        /// </summary>
        void Serialize(IMessage message, byte[] buffer, int offset);
        
        /// <summary>
        /// Deserializes message from given buffer at given offset.
        /// </summary>
        IMessage Deserialize(byte[] buffer, int offset);
        
        /// <summary>
        /// Gets message size from buffer at given offset.
        /// </summary>
        int GetSizeFromBuffer(byte[] buffer, int offset);
        
        /// <summary>
        /// Gets size of given message.
        /// </summary>
        int GetSizeFromMessage(IMessage message);
    }
    
    public interface IMessagePool
    {
        T Take<T>(PoolElementDecoratorDelegate<T> decorator = null) where T : class, IMessage, new();
        
        void Return(IMessage message);
    }
    
    /// <summary>
    /// Default implementation of <see cref="IMessagePool"/>.
    ///
    /// TODO: no initial allocations is done for the object pools, this might negatively affect the performance so monitor this and checkout if pre-allocation
    /// can fix this.
    /// </summary>
    public sealed class MessagePool : IMessagePool
    {
        #region Static fields
        private static readonly Dictionary<Type, IPool<IMessage>> Pools;
        #endregion

        static MessagePool()
        {
            // Map and reduce all generic message types to their underlying types.
            StructSerializer.Reduce<IMessage>(m => m.GetType());
            
            Pools = new Dictionary<Type, IPool<IMessage>>();
        }
            
        public MessagePool()
        {
        }
        
        public T Take<T>(PoolElementDecoratorDelegate<T> decorator = null) where T : class, IMessage, new()
        {
            var type = typeof(T);
            
            if (!StructSerializer.SupportsType(type))
                throw new InvalidOperationException($"no schema is registered for message type {type.Name}");
            
            if (!Pools.TryGetValue(type, out var pool))
            {
                pool = new CleanPool<IMessage>(
                    new DelegatePool<IMessage>(new LinearStorageObject<IMessage>(new LinearGrowthArray<IMessage>(8)), 
                                               () => new T())
                );
                
                Pools.Add(type, pool);
            }
                
            var message = (T)pool.Take();
            
            decorator?.Invoke(message);
            
            return message;
        }
        
        public void Return(IMessage message) 
            => Pools[message.GetType()].Return(message);
    }

    /// <summary>
    /// Default implementation of <see cref="IMessageSerializer"/> and default message serializer for Fracture. Uses serialization module found in Fracture for
    /// message serialization. Before using the serializer you need to setup your message schema. For this see the Fracture.Net.Serialization for help.
    ///
    /// You can also create your custom serializer that includes the schema by inheriting from this class.
    /// </summary>
    public class MessageSerializer : IMessageSerializer
    {
        public MessageSerializer()
        {
        }

        public void Serialize(IMessage message, byte[] buffer, int offset)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            
            StructSerializer.Serialize(message, buffer, offset);
        }

        public IMessage Deserialize(byte[] buffer, int offset)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            
            return (IMessage)StructSerializer.Deserialize(buffer, offset);
        }

        public int GetSizeFromBuffer(byte[] buffer, int offset)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            
            return StructSerializer.GetSizeFromBuffer(buffer, offset);
        }

        public int GetSizeFromMessage(IMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            
            return StructSerializer.GetSizeFromValue(message);
        }
    }
}