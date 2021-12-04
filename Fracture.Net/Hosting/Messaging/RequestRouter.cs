using System;
using System.Collections.Generic;
using System.Linq;
using Fracture.Net.Messages;
using NLog;

namespace Fracture.Net.Hosting.Messaging
{
    /// <summary>
    /// Delegate for creating request handler delegates. Request handler delegates are invoked all requests that have route accepting them.
    /// </summary>
    public delegate void RequestHandlerDelegate(IRequest request, IResponse response);

    /// <summary>
    /// Interface for implementing request routers that provide functionality for registering routes.
    /// </summary>
    public interface IRequestRouteConsumer
    {
        /// <summary>
        /// Registers route based on given message match. All requests that contain message that match this matcher will be handled by the given handler.
        /// </summary>
        /// <param name="match">matcher that the request message must match in order to be passed to the handler</param>
        /// <param name="handler">handler invoked when request message matches the preceding matcher</param>
        void Use(MessageMatchDelegate match, RequestHandlerDelegate handler);
    }
    
    /// <summary>
    /// Interface that provides full request routing implementation by functioning as route consumer and dispatcher.
    /// </summary>
    public interface IRequestRouter : IRequestRouteConsumer
    {
        /// <summary>
        /// Dispatch given request to any request handler that accepts it. Use the response object to get the results returned by the handler. 
        /// </summary>
        /// <param name="request">request that will be dispatched and possibly handled</param>
        /// <param name="response">response object to contain the response from handling the request</param>
        void Dispatch(IRequest request, IResponse response);
    }

    /// <summary>
    /// Default implementation of <see cref="IRequestRouter"/>.  
    /// </summary>
    public sealed class RequestRouter : IRequestRouter
    {
        #region Static fields
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion
        
        #region Private request route class
        /// <summary>
        /// Class representing single request route.
        /// </summary>
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
        
        public void Use(MessageMatchDelegate match, RequestHandlerDelegate handler)
            => routes.Add(new RequestRoute(match, handler));

        public void Dispatch(IRequest request, IResponse response)
        {
            var route = routes.FirstOrDefault(r => r.Match(request.Message));
            
            if (route != null)
            {
                try
                {
                    route.Handler(request, response);
                }
                catch (Exception e)
                {
                    if (ResponseStatus.Empty(response.StatusCode) || !ResponseStatus.IndicatesFailure(response.StatusCode))
                        response.ServerError(exception: e);
                    else
                        Log.Error(e, "unhandled error occurred while handling a request but the handler returned a response that is not empty and does not " +
                                     "indicate failure", request, response);
                }
            }
            else 
                response.NoRoute();
        }
    }
}