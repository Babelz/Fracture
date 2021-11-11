using System;
using Fracture.Common.Memory.Pools;
using Fracture.Net.Messages;
using Fracture.Net.Serialization;
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

    /// <summary>
    /// Default implementation of <see cref="IMessageSerializer"/> and default message serializer for Fracture. Uses serialization module found in Fracture for
    /// message serialization. Before using the serializer you need to setup your message schema. For this see the Fracture.Net.Serialization for help.
    ///
    /// You can also create your custom serializer that includes the schema by inheriting from this class.
    /// </summary>
    public class MessageSerializer : IMessageSerializer
    {
        #region Fields
        private readonly IArrayPool<byte> pool;
        #endregion
        
        public MessageSerializer(IArrayPool<byte> pool)
            => this.pool = pool ?? throw new ArgumentNullException(nameof(pool));

        public byte[] Serialize(IMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            
            var buffer = pool.Take(GetSizeFromMessage(message));
            
            StructSerializer.Serialize(message, buffer, 0);
            
            return buffer;
        }

        public IMessage Deserialize(byte[] buffer, int offset)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            
            // TODO: implement pooling support for messages when using Fracture serializer.
            // TODO: this can be done by passing activator function or object for the serializer to decorate.
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