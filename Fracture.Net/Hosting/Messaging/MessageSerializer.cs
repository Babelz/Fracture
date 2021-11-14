using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Fracture.Common.Collections;
using Fracture.Common.Memory.Pools;
using Fracture.Common.Memory.Storages;
using Fracture.Net.Messages;
using Fracture.Net.Serialization;
using Fracture.Net.Serialization.Generation;
using Fracture.Net.Serialization.Generation.Builders;

namespace Fracture.Net.Hosting.Messaging
{
    /// <summary>
    /// Interface for implementing message serializers. Providers layer of abstraction for serializing and deserializing with different schemas.
    /// </summary>
    public interface IMessageSerializer
    {
        /// <summary>
        /// Serializes given message to buffer and returns it to the caller.
        /// </summary>
        byte[] Serialize(IMessage message);
        
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
        IMessage Take<T>() where T : class, IMessage, new();
        
        void Return<T>(T message) where T : class, IMessage;
    }
    
    /// <summary>
    /// Default implementation of <see cref="IMessagePool"/>.
    /// </summary>
    public sealed class MessagePool : IMessagePool
    {
        #region Static fields
        private static readonly Dictionary<Type, object> Pools; 
        #endregion

        static MessagePool()
            => Pools = new Dictionary<Type, object>();

        public MessagePool()
        {
        }
        
        public IMessage Take<T>() where T : class, IMessage, new()
        {
            var type = typeof(T);
            
            if (StructSerializer.SupportsType(type))
                throw new InvalidOperationException($"no schema is registered for message type {type.Name}");
            
            if (!Pools.ContainsKey(type))
                Pools.Add(type, new CleanPool<T>(new Pool<T>(new LinearStorageObject<T>(new LinearGrowthArray<T>(8)), 0)));
            
            return ((IPool<T>)Pools[type]).Take();
        }
        
        public void Return<T>(T message) where T : class, IMessage
            => ((IPool<T>)Pools[typeof(T)]).Return(message);
    }

    /// <summary>
    /// Default implementation of <see cref="IMessageSerializer"/> and default message serializer for Fracture. Uses serialization module found in Fracture for
    /// message serialization. Before using the serializer you need to setup your message schema. For this see the Fracture.Net.Serialization for help.
    ///
    /// You can also create your custom serializer that includes the schema by inheriting from this class.
    /// </summary>
    public class MessageSerializer : IMessageSerializer
    {
        #region Fields
        private readonly IArrayPool<byte> buffers;
        #endregion
        
        public MessageSerializer(IArrayPool<byte> buffers)
            => this.buffers = buffers ?? throw new ArgumentNullException(nameof(buffers));

        public byte[] Serialize(IMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            
            var buffer = buffers.Take(GetSizeFromMessage(message));
            
            StructSerializer.Serialize(message, buffer, 0);
            
            return buffer;
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