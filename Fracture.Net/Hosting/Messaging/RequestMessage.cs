using System;
using Fracture.Net.Messages;

namespace Fracture.Net.Hosting.Messaging
{
    /// <summary>
    /// Message that represents a request received by the server from a peer.
    /// </summary>
    public sealed class RequestMessage : IMessage
    {
        #region Properties
        /// <summary>
        /// Gets or sets the message that the peer send with the request.
        /// </summary>
        public IMessage Message
        {
            get;
            set;
        }
        
        /// <summary>
        /// Gets or sets the raw contents the peer send with the request.
        /// </summary>
        public byte[] Contents
        {
            get;
            set;
        }
        #endregion

        public RequestMessage()
        {
        }

        public void Clear()
        {
            Message  = default;
            Contents = default;
        }
    }
}