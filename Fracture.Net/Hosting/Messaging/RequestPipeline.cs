using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Fracture.Common.Collections;
using Fracture.Net.Hosting.Peers;
using Fracture.Net.Messages;

namespace Fracture.Net.Hosting.Messaging
{
    /// <summary>
    /// Structure representing request object that contains a message receive from a peer. 
    /// </summary>
    public struct Request
    {
        #region Properties
        /// <summary>
        /// Gets the peer that created this request.
        /// </summary>
        public PeerConnection Peer
        {
            get;
            set;
        }
        
        /// <summary>
        /// Gets the request contents in it's raw serialized format.
        /// </summary>
        public byte[] Contents
        {
            get;
            set;
        }
     
        /// <summary>
        /// Gets the request contents in it's deserialized format.
        /// </summary>
        public IMessage Message
        {
            get;
            set;
        }
        #endregion
    }

    /// <summary>
    /// Delegate used for incoming request handling.
    /// </summary>
    public delegate Response RequestHandlerDelegate(in Request request);
    
    /// <summary>
    /// Interface for implementing type based request routers. Requests are routed based on message matchers associated with the handlers.  
    /// </summary>
    public interface IRequestRouter
    {
        /// <summary>
        /// Registers new message router. All messages that match the specified matcher will be handled by the supplied handler.
        /// </summary>
        void Route(MessageMatchDelegate match, RequestHandlerDelegate handler);
    }
    
    /// <summary>
    /// Delegate that is used to handle enqueued requests.
    /// </summary>
    public delegate void RequestResponseHandlerDelegate(in Request request, in Response response);
    
    /// <summary>
    /// Interface for implementing request handlers for processing enqueued requests.
    /// </summary>
    public interface IRequestHandler
    {
        /// <summary>
        /// Enqueue incoming request for processing.
        /// </summary>
        /// <param name="peer">peer that send this request</param>
        /// <param name="contents">raw contents received from the peer</param>
        /// <param name="message">message received from the peer with the request</param>
        void Enqueue(in PeerConnection peer, byte[] contents, IMessage message);
        
        /// <summary>
        /// Handles all enqueued requests using given request handler.
        /// </summary>
        void Handle(RequestResponseHandlerDelegate handler);
    }

    public sealed class RequestPipeline : IRequestRouter, IRequestHandler
    {
        #region Typed route class
        private sealed class RouteContext
        {
            #region Properties
            public MessageMatchDelegate Match
            {
                get;
            }

            public RequestHandlerDelegate Handler
            {
                get;
            }
            #endregion

            public RouteContext(MessageMatchDelegate match, RequestHandlerDelegate handler)
            {
                Match   = match ?? throw new ArgumentNullException(nameof(match));
                Handler = handler ?? throw new ArgumentNullException(nameof(handler));
            }
        }
        #endregion
        
        #region Fields
        private readonly List<RouteContext> routes;
        
        private readonly LinearGrowthArray<Request> requests;
        
        private int count;
        #endregion
        
        public RequestPipeline(int initialRequestsCapacity)
        {
            routes   = new List<RouteContext>();
            requests = new LinearGrowthArray<Request>(initialRequestsCapacity);
        }

        private void EnsureCapacity()
        {
            if (count >= requests.Length)
                requests.Grow();
        }
        
        public void Route(MessageMatchDelegate match, RequestHandlerDelegate handler)
            => routes.Add(new RouteContext(match, handler));

        public void Enqueue(in PeerConnection peer, byte[] contents, IMessage message)
        {
            EnsureCapacity();
            
            if (contents == null)
                throw new ArgumentException(nameof(contents));
            
            if (message == null)
                throw new ArgumentException(nameof(message));
            
            ref var request = ref requests.AtIndex(count++);
            
            request.Peer     = peer;
            request.Contents = contents;
            request.Message  = message;
        }

        public void Handle(RequestResponseHandlerDelegate handler)
        {
            if (handler == null)
                throw new ArgumentException(nameof(handler));
            
            for (var i = 0; i < count; i++)
                routes.FirstOrDefault(r => r.Match(requests.AtIndex(i).Message))?.Handler(requests.AtIndex(i));
            
            count = 0;
        }
    }
}