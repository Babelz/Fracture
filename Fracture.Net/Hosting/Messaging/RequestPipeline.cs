using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Runtime.CompilerServices;
using Fracture.Common.Collections;
using Fracture.Common.Memory;
using Fracture.Net.Hosting.Peers;
using Fracture.Net.Messages;
using Microsoft.Diagnostics.Tracing.Parsers.AspNet;

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
        #endregion

        public Request()
        {
        }
        
        public void Clear()
        {
            Peer     = default;
            Contents = default;
            Message  = default;
        }
    }
    
    
    /// <summary>
    /// Delegate for creating request match delegates. Request matchers are used in request middleware.
    /// </summary>
    public delegate bool RequestMatchDelegate(IRequest request);
    
    /// <summary>
    /// Static utility class containing request matching utilities.
    /// </summary>
    public static class RequestMatch
    {
        /// <summary>
        /// Matcher that accepts requests of any kind.
        /// </summary>
        public static RequestMatchDelegate Any() => (_) => true;
        
        /// <summary>
        /// Matcher that uses message based matching using the message contained in the request.
        /// </summary>
        public static RequestMatchDelegate Message(MessageMatchDelegate match)
            => (request) => match(request.Message);
        
        /// <summary>
        /// Matcher that uses conditional matching using given conditional delegate.
        /// </summary>
        public static RequestMatchDelegate Condition(Func<bool> condition)   
            => (_) => condition();

        /// <summary>
        /// Matcher that matches all request that match given predicate.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RequestMatchDelegate Match(Func<IRequest, bool> predicate) 
            => (request) => predicate(request);
    }
    
    public delegate void RequestMiddlewareHandlerDelegate(IRequest request, out bool pass);

    public interface IRequestMiddlewareConsumer
    {
        void Use(RequestMatchDelegate match, RequestMiddlewareHandlerDelegate handler);
    }
    
    public interface IRequestMiddlewareHandler
    {
        bool Pass(IRequest request);
    }

    public sealed class RequestMiddlewareHandler : IRequestMiddlewareHandler, IRequestMiddlewareConsumer
    {
        #region Private request middleware class
        private sealed class RequestMiddleware
        {
            #region Properties
            public RequestMatchDelegate Match
            {
                get;
            }

            public RequestMiddlewareHandlerDelegate Handler
            {
                get;
            }
            #endregion

            public RequestMiddleware(RequestMatchDelegate match, RequestMiddlewareHandlerDelegate handler)
            {
                Match   = match ?? throw new ArgumentNullException(nameof(match));
                Handler = handler ?? throw new ArgumentNullException(nameof(handler));
            }
        }
        #endregion

        #region Fields
        private readonly List<RequestMiddleware> middlewares;
        #endregion

        public RequestMiddlewareHandler()
            => middlewares = new List<RequestMiddleware>();

        public void Use(RequestMatchDelegate match, RequestMiddlewareHandlerDelegate handler)
            => middlewares.Add(new RequestMiddleware(match, handler));
        
        public bool Pass(IRequest request)
        {
            foreach (var middleware in middlewares.Where(m => m.Match(request)))
            {
                middleware.Handler(request, out var pass);
                
                if (pass)
                    return true;
            }
            
            return false;
        }
    }
    
    public delegate void RequestHandlerDelegate(IRequest request, IResponse response);
    
    public interface IRequestRouter
    {
        void Route(MessageMatchDelegate match, RequestHandlerDelegate handler);
    }
    
    public interface IRequestDispatcher
    {
        void Dispatch(IRequest request, IResponseDecorator response);
    }

    public sealed class RequestRouter : IRequestRouter, IRequestDispatcher
    {
        #region Private request route class
        private sealed class RequestRoute
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

            public RequestRoute(MessageMatchDelegate match, RequestHandlerDelegate handler)
            {
                Match   = match ?? throw new ArgumentNullException(nameof(match));
                Handler = handler ?? throw new ArgumentNullException(nameof(handler));
            }
        }
        #endregion
        
        #region Fields
        private readonly List<RequestRoute> routes;
        #endregion

        public RequestRouter()
            => routes = new List<RequestRoute>();
        
        public void Route(MessageMatchDelegate match, RequestHandlerDelegate handler)
            => routes.Add(new RequestRoute(match, handler));

        public void Dispatch(IRequest request, IResponseDecorator response)
        {
            foreach (var route in routes)
            {
                if (!route.Match(request.Message))
                    continue;
                
                try
                {
                    route.Handler(request, response);
                }
                catch (Exception e)
                {
                    response.ServerError(exception: e);
                }
                
                return;
            }
            
            response.NoRoute();
        }
    }
}