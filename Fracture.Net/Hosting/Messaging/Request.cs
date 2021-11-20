using System;
using Fracture.Common.Memory;
using Fracture.Net.Hosting.Peers;
using Fracture.Net.Messages;

namespace Fracture.Net.Hosting.Messaging
{
    /// <summary>
    /// Interface representing request object that contains a message received from a peer. 
    /// </summary>
    public interface IRequest
    {
        #region Properties
        /// <summary>
        /// Gets the peer that created this request.
        /// </summary>
        PeerConnection Peer
        {
            get;
        }
        
        /// <summary>
        /// Gets the request contents in it's raw serialized format.
        /// </summary>
        byte[] Contents
        {
            get;
        }
     
        /// <summary>
        /// Gets the request contents in it's deserialized format.
        /// </summary>
        IMessage Message
        {
            get;
        }
        #endregion
    }
    
    /// <summary>
    /// Structure containing middleware request context of single request object. 
    /// </summary>
    public readonly struct RequestMiddlewareContext : IMiddlewareRequestContext
    {
        #region Properties
        /// <summary>
        /// Gets the request associated with the middleware request.
        /// </summary>
        public IRequest Request
        {
            get;
        }
        #endregion

        public RequestMiddlewareContext(IRequest request)
            => Request = request ?? throw new ArgumentNullException(nameof(request));
    }

    /// <summary>
    /// Default implementation of <see cref="IRequest"/>. This implementation can be pooled and thus is mutable.
    /// </summary>
    public sealed class Request : IRequest, IClearable
    {
        #region Properties
        public PeerConnection Peer
        {
            get;
            set;
        }

        public byte[] Contents
        {
            get;
            set;
        }

        public IMessage Message
        {
            get;
            set;
        }
        
        public TimeSpan Timestamp
        {
            get;
            set;
        }
        #endregion

        public Request()
        {
        }
        
        public void Clear()
        {
            Peer      = default;
            Contents  = default;
            Message   = default;
            Timestamp = default;
        }
    }
}