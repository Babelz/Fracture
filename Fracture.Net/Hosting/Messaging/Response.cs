using System;
using System.Runtime.CompilerServices;
using Fracture.Common.Collections;
using Fracture.Common.Memory;
using Fracture.Common.Memory.Pools;
using Fracture.Common.Memory.Storages;
using Fracture.Net.Hosting.Peers;
using Fracture.Net.Messages;

namespace Fracture.Net.Hosting.Messaging
{
    /// <summary>
    /// Enumeration defining all possible response status codes.
    /// </summary>
    public enum ResponseStatusCode : byte
    {
        /// <summary>
        /// Request did not return any response. 
        /// </summary>
        Empty = 0,
        
        /// <summary>
        /// Request has handled successfully.
        /// </summary>
        Ok,
        
        /// <summary>
        /// Error occurred inside the handler while handling the message.
        /// </summary>
        ServerError,
        
        /// <summary>
        /// Request received from the peer was badly formatted or invalid.
        /// </summary>
        BadRequest,
        
        /// <summary>
        /// Request received from the peer had no route.
        /// </summary>
        NoRoute,
        
        /// <summary>
        /// Peer connection should be reset. 
        /// </summary>
        Reset
    }
    
    /// <summary>
    /// Interface representing response object returned by request handlers.
    /// </summary>
    public interface IResponse
    {
        #region Properties
        /// <summary>
        /// Gets the status code of this response.
        /// </summary>
        ResponseStatusCode StatusCode
        {
            get;
        }
        
        /// <summary>
        /// Gets the response message.
        /// </summary>
        IMessage Message
        {
            get;
        }
        
        /// <summary>
        /// Gets exception that occurred during request handling.
        /// </summary>
        Exception Exception
        {
            get;
        }
        
        /// <summary>
        /// Returns boolean declaring whether this response contains exception.
        /// </summary>
        bool ContainsException
        {
            get;
        }
        
        /// <summary>
        /// Returns boolean declaring whether this response contains reply.
        /// </summary>
        bool ContainsReply
        {
            get;
        }
        #endregion
        
        /// <summary>
        /// Decorates the response object to contain successful response.
        /// </summary>
        /// <param name="message">optional message associated with the response</param>
        void Ok(IMessage message = null);
        
        /// <summary>
        /// Decorates the response object to contain server error response.
        /// </summary>
        /// <param name="message">optional message associated with the response</param>
        /// <param name="exception">optional exception associated with the response</param>
        void ServerError(IMessage message = null, Exception exception = null);
        
        /// <summary>
        /// Decorates the response object to contain bad request error response.
        /// </summary>
        /// <param name="message">optional message associated with the response</param>
        /// <param name="exception">optional exception associated with the response</param>
        void BadRequest(IMessage message = null, Exception exception = null);

        /// <summary>
        /// Decorates the response object to contain peer reset response. 
        /// </summary>
        /// <param name="message">optional message associated with the response</param>
        /// <param name="exception">optional exception associated with the response</param>
        void Reset(IMessage message = null, Exception exception = null);
        
        /// <summary>
        /// Decorates the response object to contain no route response.
        /// </summary>
        void NoRoute();
    }
    
    /// <summary>
    /// Default implementation of <see cref="IResponse"/>. This implementation can be pooled and thus is mutable.
    /// </summary>
    public sealed class Response : IResponse, IClearable
    {
        #region Properties
        public ResponseStatusCode StatusCode
        {
            get;
            set;
        }
        
        public IMessage Message
        {
            get;
            set;
        }
        
        public Exception Exception
        {
            get;
            set;
        }
        
        public bool ContainsException => Exception != null;
        
        public bool ContainsReply => Message != null;
        #endregion

        public Response()
        {
        }
        
        private void AssertEmpty()
        {
            if (StatusCode != ResponseStatusCode.Empty)
                throw new InvalidOperationException("request is not empty");
        }
        
        public void Ok(IMessage message = null)
        {
            AssertEmpty();
               
            Message    = message;
            StatusCode = ResponseStatusCode.Ok;
        }

        public void ServerError(IMessage message = null, Exception exception = null)
        {
            AssertEmpty();

            Message    = message;
            Exception  = exception;
            StatusCode = ResponseStatusCode.ServerError;
        }

        public void BadRequest(IMessage message = null, Exception exception = null)
        {
            AssertEmpty();

            Message    = message;
            Exception  = exception;
            StatusCode = ResponseStatusCode.BadRequest;
        }
        
        public void Reset(IMessage message = null, Exception exception = null)
        {
            AssertEmpty();

            Message    = message;
            Exception  = exception;
            StatusCode = ResponseStatusCode.Reset;
        }
        
        public void NoRoute()
        {
            AssertEmpty();
            
            StatusCode = ResponseStatusCode.NoRoute;
        }
            
        public void Clear()
        {
            StatusCode = default;
            Message    = default;
            Exception  = default;
        }
    }
}