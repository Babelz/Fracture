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
    /// Class that provides full request routing implementation by functioning as route consumer and dispatcher.  
    /// </summary>
    public sealed class RequestRouter : IRequestRouter, IRequestDispatcher
    {
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
        
        public void Route(MessageMatchDelegate match, RequestHandlerDelegate handler)
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
                    response.ServerError(exception: e);
                }
            }
            else 
                response.NoRoute();
        }
    }
}