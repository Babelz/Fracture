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
    
    /// <summary>
    /// Delegate for creating request middleware delegates. Middleware delegates are invoked for each registered middleware that match the request.
    /// </summary>
    public delegate void RequestMiddlewareHandlerDelegate(IRequest request, out bool reject);

    /// <summary>
    /// Interface for implementing request middleware consumers that are used for registering middleware callbacks.
    /// </summary>
    public interface IRequestMiddlewareConsumer
    {
        /// <summary>
        /// Register given middleware to the pipeline. All requests that match the given matcher will be handled by given middleware delegate.
        /// </summary>
        /// <param name="match">matcher that the request must match in order to be passed to the handler</param>
        /// <param name="handler">handler invoked when request matches the preceding matcher</param>
        void Use(RequestMatchDelegate match, RequestMiddlewareHandlerDelegate handler);
    }
    
    /// <summary>
    /// Interface for implementing request middleware invokers that handle running the middleware pipeline for requests.
    /// </summary>
    public interface IRequestMiddlewareInvoker
    {
        /// <summary>
        /// Invoke the pipeline for given request. 
        /// </summary>
        /// <param name="request">request that the pipeline will be executed for</param>
        /// <returns>boolean declaring whether the request was rejected by the pipeline</returns>
        bool Invoke(IRequest request);
    }

    /// <summary>
    /// Class that provides full middleware pipeline implementation by functioning as middleware consumer and invoker.  
    /// </summary>
    public sealed class RequestMiddlewarePipeline : IRequestMiddlewareInvoker, IRequestMiddlewareConsumer
    {
        #region Private request middleware class
        /// <summary>
        /// Private context class containing single request middleware.
        /// </summary>
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

        public RequestMiddlewarePipeline()
            => middlewares = new List<RequestMiddleware>();

        public void Use(RequestMatchDelegate match, RequestMiddlewareHandlerDelegate handler)
            => middlewares.Add(new RequestMiddleware(match, handler));
        
        public bool Invoke(IRequest request)
        {
            // Go trough all middlewares that match given request.
            foreach (var middleware in middlewares.Where(m => m.Match(request)))
            {
                // Invoke the middleware that matched given request.
                middleware.Handler(request, out var reject);
                
                // Immediately return if the middleware rejected the request.
                if (!reject)
                    return false;
            }
            
            return true;
        }
    }

    /// <summary>
    /// Delegate for creating request handler delegates. Request handler delegates are invoked all requests that have route accepting them.
    /// </summary>
    public delegate void RequestHandlerDelegate(IRequest request, IResponse response);
    
    /// <summary>
    /// Interface for implementing request routers that provide functionality for registering routes.
    /// </summary>
    public interface IRequestRouter
    {
        /// <summary>
        /// Registers route based on given message match. All requests that contain message that match this matcher will be handled by the given handler.
        /// </summary>
        /// <param name="match">matcher that the request message must match in order to be passed to the handler</param>
        /// <param name="handler">handler invoked when request message matches the preceding matcher</param>
        void Route(MessageMatchDelegate match, RequestHandlerDelegate handler);
        
        /// <summary>
        /// Registers route based on given request match. All requests that match this matcher will be handled by the given handler.
        /// </summary>
        /// <param name="match">matcher that the request must match in order to be passed to the handler</param>
        /// <param name="handler">handler invoked when request matches the preceding matcher</param>
        void Route(RequestMatchDelegate match, RequestHandlerDelegate handler);
    }
    
    /// <summary>
    /// Interface for implementing request dispatchers that delegate handling of received requests to registered request handlers.
    /// </summary>
    public interface IRequestDispatcher
    {
        /// <summary>
        /// Dispatch given request to any request handler that accepts it. Use the response object to get the results returned by the handler. 
        /// </summary>
        /// <param name="request">request that will be dispatched and possibly handled</param>
        /// <param name="response">response object to contain the response from handling the request</param>
        void Dispatch(IRequest request, IResponse response);
    }

    /// <summary>
    /// Class that provides full request pipeline implementation by functioning as router and request dispatcher.  
    /// </summary>
    public sealed class RequestPipeline : IRequestRouter, IRequestDispatcher
    {
        #region Private request route class
        /// <summary>
        /// Class representing single request route.
        /// </summary>
        private sealed class RequestRoute
        {
            #region Properties
            public RequestMatchDelegate Match
            {
                get;
            }

            public RequestHandlerDelegate Handler
            {
                get;
            }
            #endregion

            public RequestRoute(RequestMatchDelegate match, RequestHandlerDelegate handler)
            {
                Match   = match ?? throw new ArgumentNullException(nameof(match));
                Handler = handler ?? throw new ArgumentNullException(nameof(handler));
            }
        }
        #endregion
        
        #region Fields
        private readonly List<RequestRoute> routes;
        #endregion

        public RequestPipeline()
            => routes = new List<RequestRoute>();
        
        public void Route(MessageMatchDelegate match, RequestHandlerDelegate handler)
            => routes.Add(new RequestRoute(RequestMatch.Message(match), handler));
        
        public void Route(RequestMatchDelegate match, RequestHandlerDelegate handler)
            => routes.Add(new RequestRoute(match, handler));

        public void Dispatch(IRequest request, IResponse response)
        {
            var route = routes.FirstOrDefault(r => r.Match(request));
            
            if (route != null)
            {
                try
                {
                    route.Handler(request, response);
                }
                catch (Exception e)
                {
                    response.ServerError(exception: e);
                }
            }
            else 
                response.NoRoute();
        }
    }
}