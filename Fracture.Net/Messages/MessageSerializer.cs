using System;
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
        ushort GetSizeFromBuffer(byte[] buffer, int offset);
        
        /// <summary>
        /// Gets size of given message.
        /// </summary>
        ushort GetSizeFromMessage(IMessage message);
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

        public ushort GetSizeFromBuffer(byte[] buffer, int offset)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            
            return StructSerializer.GetSizeFromBuffer(buffer, offset);
        }

        public ushort GetSizeFromMessage(IMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            
            return StructSerializer.GetSizeFromValue(message);
        }
    }
}