using Fracture.Common.Memory;
using Fracture.Net.Hosting.Peers;
using Fracture.Net.Messages;

namespace Fracture.Net.Hosting.Requests
{
    /// <summary>
    /// Message that represents a request received by the server from a peer.
    /// </summary>
    public sealed class RequestMessage : IMessage
    {
        #region Properties
        /// <summary>
        /// Gets or sets the peer connection details that has send this messages.
        /// </summary>
        public PeerConnection Peer
        {
            get;
            set;
        }
        
        /// <summary>
        /// Gets or sets the path of the request. This is the path the peer has send the message to.
        /// </summary>
        public string Path
        {
            get;
            set;
        }
        
        /// <summary>
        /// Gets or sets the contents of the request. This contains the message send by the peer in serialized format.
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
            Peer     = default;
            Path     = default;
            Contents = default;
        }
    }
}