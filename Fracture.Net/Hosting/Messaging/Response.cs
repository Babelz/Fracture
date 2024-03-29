using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Fracture.Common.Collections;
using Fracture.Common.Memory;
using Fracture.Common.Memory.Pools;
using Fracture.Common.Memory.Storages;
using Fracture.Common.Util;
using Fracture.Net.Messages;
using Newtonsoft.Json;

namespace Fracture.Net.Hosting.Messaging
{
    public static class ResponseStatus
    {
        /// <summary>
        /// Enumeration defining all possible response status codes.
        /// </summary>
        public enum Code : byte
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
            Reset,
        }

        #region Static fields
        private static readonly HashSet<Code> SuccessfulStatusCodes = new HashSet<Code>
        {
            Code.Ok,
            Code.Reset,
        };

        private static readonly HashSet<Code> UnsuccessfulStatusCodes = new HashSet<Code>
        {
            Code.ServerError,
            Code.BadRequest,
            Code.NoRoute,
        };
        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Empty(Code code)
            => code == Code.Empty;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IndicatesSuccess(Code code)
            => SuccessfulStatusCodes.Contains(code);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IndicatesFailure(Code code)
            => UnsuccessfulStatusCodes.Contains(code);
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
        ResponseStatus.Code StatusCode
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
        void Ok(in IMessage message = null);

        /// <summary>
        /// Decorates the response object to contain server error response.
        /// </summary>
        /// <param name="message">optional message associated with the response</param>
        /// <param name="exception">optional exception associated with the response</param>
        void ServerError(in IMessage message = null, Exception exception = null);

        /// <summary>
        /// Decorates the response object to contain bad request error response.
        /// </summary>
        /// <param name="message">optional message associated with the response</param>
        /// <param name="exception">optional exception associated with the response</param>
        void BadRequest(in IMessage message = null, Exception exception = null);

        /// <summary>
        /// Decorates the response object to contain peer reset response. 
        /// </summary>
        /// <param name="message">optional message associated with the response</param>
        /// <param name="exception">optional exception associated with the response</param>
        void Reset(in IMessage message = null, Exception exception = null);

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
        #region Static fields
        private static readonly IPool<Response> Pool = new ConcurrentPool<Response>(
            new CleanPool<Response>(
                new Pool<Response>(new LinearStorageObject<Response>(new LinearGrowthArray<Response>(128)), 128))
        );
        #endregion

        #region Properties
        public ResponseStatus.Code StatusCode
        {
            get;
            private set;
        }

        public IMessage Message
        {
            get;
            private set;
        }

        public Exception Exception
        {
            get;
            private set;
        }

        public bool ContainsException => Exception != null;

        public bool ContainsReply => Message != null;
        #endregion

        public Response()
        {
        }

        private void AssertEmpty()
        {
            if (!ResponseStatus.Empty(StatusCode))
                throw new InvalidOperationException("response is not empty");
        }

        public void Ok(in IMessage message = null)
        {
            AssertEmpty();

            Message    = message;
            StatusCode = ResponseStatus.Code.Ok;
        }

        public void ServerError(in IMessage message = null, Exception exception = null)
        {
            AssertEmpty();

            Message    = message;
            Exception  = exception;
            StatusCode = ResponseStatus.Code.ServerError;
        }

        public void BadRequest(in IMessage message = null, Exception exception = null)
        {
            AssertEmpty();

            Message    = message;
            Exception  = exception;
            StatusCode = ResponseStatus.Code.BadRequest;
        }

        public void Reset(in IMessage message = null, Exception exception = null)
        {
            AssertEmpty();

            Message    = message;
            Exception  = exception;
            StatusCode = ResponseStatus.Code.Reset;
        }

        public void NoRoute()
        {
            AssertEmpty();

            StatusCode = ResponseStatus.Code.NoRoute;
        }

        public void Clear()
        {
            StatusCode = default;
            Message    = default;
            Exception  = default;
        }

        public override string ToString()
            => JsonConvert.SerializeObject(this);

        public override int GetHashCode()
            => HashUtils.Create()
                        .Append(StatusCode)
                        .Append(Message)
                        .Append(Exception);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Response Take()
            => Pool.Take();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Return(Response response)
            => Pool.Return(response);
    }
}