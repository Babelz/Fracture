using System;
using System.Security.Cryptography.X509Certificates;
using Fracture.Common.Memory;

namespace Fracture.Net.Messages
{
    /// <summary>
    /// Interface for implementing messages. Messages are passed between servers and clients.
    /// </summary>
    public interface IMessage
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
    /// Message that represents a request received by the server from a peer.
    /// </summary>
    public sealed class RequestMessage : IMessage, IClearable
    {
        #region Properties
        /// <summary>
        /// Gets or sets the path of the request. This is the path the client has send the message to.
        /// </summary>
        public string Path
        {
            get;
            set;
        }
        
        /// <summary>
        /// Gets or sets the contents of the request. This contains the message send by the client in serialized format.
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
            Path     = default;
            Contents = default;
        }
    }
}