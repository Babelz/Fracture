using Fracture.Net.Messages;

namespace Fracture.Net.Hosting.Messages
{
    /// <summary>
    /// Message that represents a request received by the server from a peer.
    /// </summary>
    public struct RequestMessage : IMessage
    {
        #region Properties
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

        public void Clear()
        {
            Path     = default;
            Contents = default;
        }
    }
}