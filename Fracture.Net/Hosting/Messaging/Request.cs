using System;
using System.Runtime.CompilerServices;
using Fracture.Common.Collections;
using Fracture.Common.Memory;
using Fracture.Common.Memory.Pools;
using Fracture.Common.Memory.Storages;
using Fracture.Common.Util;
using Fracture.Net.Hosting.Servers;
using Fracture.Net.Messages;
using Newtonsoft.Json;

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
        /// Gets the request contents in its raw serialized format.
        /// </summary>
        byte[] Contents
        {
            get;
        }
     
        /// <summary>
        /// Gets the request contents in its deserialized format.
        /// </summary>
        IMessage Message
        {
            get;
        }
        #endregion
    }
    
    /// <summary>
    /// Structure containing middleware request context of single request object. 
    /// </summary>
    public readonly struct RequestMiddlewareContext : IMiddlewareRequestContext
    {
        #region Properties
        /// <summary>
        /// Gets the request associated with the middleware request.
        /// </summary>
        public IRequest Request
        {
            get;
        }
        #endregion

        public RequestMiddlewareContext(IRequest request)
            => Request = request ?? throw new ArgumentNullException(nameof(request));
    }

    /// <summary>
    /// Default implementation of <see cref="IRequest"/>. This implementation can be pooled and thus is mutable.
    /// </summary>
    public sealed class Request : IRequest, IClearable
    {
        #region Static fields
        private static readonly IPool<Request> Pool = new ConcurrentPool<Request>(
            new CleanPool<Request>(
                new Pool<Request>(new LinearStorageObject<Request>(new LinearGrowthArray<Request>(128)), 128))
        );
        #endregion
        
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
        
        public TimeSpan Timestamp
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
            Peer      = default;
            Contents  = default;
            Message   = default;
            Timestamp = default;
        }

        public override string ToString()
            => JsonConvert.SerializeObject(this);

        public override int GetHashCode()
            => HashUtils.Create()
                        .Append(Peer)
                        .Append(Contents)
                        .Append(Message)
                        .Append(Timestamp);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Request Take() => Pool.Take();
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Return(Request request) => Pool.Return(request);
    }
}