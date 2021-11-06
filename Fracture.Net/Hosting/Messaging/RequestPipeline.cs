using System;
using Fracture.Net.Hosting.Peers;
using Fracture.Net.Messages;

namespace Fracture.Net.Hosting.Messaging
{
    public readonly struct Request 
    {
        #region Properties
        public PeerConnection Peer
        {
            get;
        }

        public byte[] Contents
        {
            get;
        }
        #endregion

        public Request(in PeerConnection peer, byte[] contents)
        {
            Peer     = peer;
            Contents = contents;
        }
    }
    
    public readonly struct Request<T>
    {
        #region Properties
        public PeerConnection Peer
        {
            get;
        }
        
        public byte[] Contents
        {
            get;
        }
     
        public T Message
        {
            get;
        }
        #endregion

        public Request(in PeerConnection peer, byte[] contents, T message)
        {
            Peer     = peer;
            Contents = contents;
            Message  = message;
        }
    }

    public delegate Response HandleRequestDelegate(in Request request);
    public delegate Response HandleRequestDelegate<T>(in Request<T> request) where T : IMessage;
    
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RequestHandlerAttribute : Attribute
    {
        #region Properties
        public Type Path
        {
            get;
        }
        #endregion

        public RequestHandlerAttribute(Type path)
            => Path = path ?? throw new ArgumentNullException(nameof(path));
    }

    public sealed class RequestPipeline
    {
        public RequestPipeline()
        {
        }
    }
}