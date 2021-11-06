using System;
using System.Runtime.CompilerServices;
using Fracture.Net.Hosting.Peers;
using Fracture.Net.Messages;

namespace Fracture.Net.Hosting.Messaging
{
    /// <summary>
    /// Structure representing request object that contains a message receive from a peer.
    /// </summary>
    public readonly struct Request
    {
        #region Properties
        /// <summary>
        /// Gets the peer that created this request.
        /// </summary>
        public PeerConnection Peer
        {
            get;
        }
        
        /// <summary>
        /// Gets the request contents in it's raw serialized format.
        /// </summary>
        public byte[] Contents
        {
            get;
        }
     
        /// <summary>
        /// Gets the request contents in it's deserialized format.
        /// </summary>
        public IMessage Message
        {
            get;
        }
        #endregion

        public Request(in PeerConnection peer, byte[] contents, IMessage message)
        {
            Peer     = peer;
            Contents = contents ?? throw new ArgumentNullException(nameof(contents));
            Message  = message ?? throw new ArgumentNullException(nameof(message));
        }
    }

    public delegate Response RequestHandlerDelegate(in Request request);
    
    public interface IRequestRouter
    {
        void Route(MessageMatchDelegate match, RequestHandlerDelegate handler);
    }
    
    public interface IRequestHandler
    {
        Response Handle(in Request request);
    }

    public sealed class RequestPipeline : IRequestRouter, IRequestHandler
    {
        public RequestPipeline()
        {
        }

        public void Route(MessageMatchDelegate match, RequestHandlerDelegate handler)
        {
            throw new NotImplementedException();
        }

        public Response Handle(in Request request)
        {
            throw new NotImplementedException();
        }
    }
}