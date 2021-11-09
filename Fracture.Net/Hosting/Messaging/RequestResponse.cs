using System;

namespace Fracture.Net.Hosting.Messaging
{
    /// <summary>
    /// Structure containing middleware request context for single request response pair object.
    /// </summary>
    public readonly struct RequestResponseMiddlewareContext : IMiddlewareRequestContext
    {
        #region Properties
        /// <summary>
        /// Gets the request part of the request response pair associated with the middleware request.
        /// </summary>
        public IRequest Request
        {
            get;
        }
        
        /// <summary>
        /// Gets the response part of the request response pair associated with the middleware request.
        /// </summary>
        public IResponse Response
        {
            get;
        }
        #endregion

        public RequestResponseMiddlewareContext(IRequest request, IResponse response)
        {
            Request  = request ?? throw new ArgumentNullException(nameof(request));
            Response = response ?? throw new ArgumentNullException(nameof(response));
        }
    }
}