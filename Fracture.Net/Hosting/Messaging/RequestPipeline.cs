using System;
using System.Runtime.CompilerServices;
using Fracture.Net.Hosting.Peers;
using Fracture.Net.Messages;

namespace Fracture.Net.Hosting.Messaging
{
    /// <summary>
    /// Enumeration defining all possible response status codes.
    /// </summary>
    public enum StatusCode : byte
    {
        /// <summary>
        /// Request has handled successfully.
        /// </summary>
        Ok = 0,
        
        /// <summary>
        /// Error occurred inside the handler while handling the message.
        /// </summary>
        ServerError,
        
        /// <summary>
        /// Request received from the peer was badly formatted or invalid.
        /// </summary>
        BadRequest,
        
        /// <summary>
        /// Peer connection should be reset. 
        /// </summary>
        Reset
    }
    
    /// <summary>
    /// Structure representing response object returned by request handlers.
    /// </summary>
    public readonly struct Response
    {
        #region Properties
        /// <summary>
        /// Gets the status code of this response.
        /// </summary>
        public StatusCode Status
        {
            get;
        }
        
        /// <summary>
        /// Gets the response message.
        /// </summary>
        public IMessage Message
        {
            get;
        }
        
        /// <summary>
        /// Gets exception that occurred during request handling.
        /// </summary>
        public Exception Exception
        {
            get;
        }
        
        /// <summary>
        /// Returns boolean declaring whether this response contains exception.
        /// </summary>
        public bool ContainsException => Exception != null;
        
        /// <summary>
        /// Returns boolean declaring whether this response contains reply.
        /// </summary>
        public bool ContainsReply => Message != null;
        #endregion
        
        private Response(StatusCode status, IMessage message, Exception exception)
        {
            Status    = status;
            Message   = message;
            Exception = exception;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Response Ok(IMessage message = null)
            => new Response(StatusCode.Ok, message, null);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Response ServerError(IMessage message = null, Exception exception = null)
            => new Response(StatusCode.ServerError, message, exception);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Response BadRequest(IMessage message = null, Exception exception = null)
            => new Response(StatusCode.BadRequest, message, exception);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Response Reset(IMessage message = null, Exception exception = null)
            => new Response(StatusCode.Reset, message, exception);
    }
    
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