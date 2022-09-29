using System;
using System.Runtime.CompilerServices;
using Fracture.Net.Hosting.Servers;
using Fracture.Net.Messages;

namespace Fracture.Net.Hosting.Messaging
{
    /// <summary>
    /// Structure containing request context for single request response pair object.
    /// </summary>
    public readonly struct RequestResponse
    {
        #region Properties
        /// <summary>
        /// Gets the request part of the request response pair.
        /// </summary>
        public Request Request
        {
            get;
        }

        /// <summary>
        /// Gets the response part of the request response pair.
        /// </summary>
        public Response Response
        {
            get;
        }
        #endregion

        public RequestResponse(Request request, Response response)
        {
            Request  = request ?? throw new ArgumentNullException(nameof(request));
            Response = response ?? throw new ArgumentNullException(nameof(response));
        }
    }

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

    /// <summary>
    /// Static utility class containing request response middleware context matching utilities. 
    /// </summary>
    public static class RequestResponseMiddlewareMatch
    {
        /// <summary>
        /// Matcher that accepts any message type and kind.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MiddlewareMatchDelegate<RequestResponseMiddlewareContext> Any()
            => delegate { return true; };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MiddlewareMatchDelegate<RequestResponseMiddlewareContext> Request(Predicate<IRequest> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            return (in RequestResponseMiddlewareContext context) => predicate(context.Request);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MiddlewareMatchDelegate<RequestResponseMiddlewareContext> Response(Predicate<IResponse> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            return (in RequestResponseMiddlewareContext context) => predicate(context.Response);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MiddlewareMatchDelegate<RequestResponseMiddlewareContext> Peer(Predicate<PeerConnection> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            return (in RequestResponseMiddlewareContext context) => predicate(context.Request.Connection);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MiddlewareMatchDelegate<RequestResponseMiddlewareContext> RequestMessage(MessageMatchDelegate match)
        {
            if (match == null)
                throw new ArgumentNullException(nameof(match));

            return (in RequestResponseMiddlewareContext context) => match(context.Request.Message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MiddlewareMatchDelegate<RequestResponseMiddlewareContext> ResponseMessage(MessageMatchDelegate match)
        {
            if (match == null)
                throw new ArgumentNullException(nameof(match));

            return (in RequestResponseMiddlewareContext context) => match(context.Response.Message);
        }
    }
}