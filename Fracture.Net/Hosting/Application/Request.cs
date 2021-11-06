using Fracture.Net.Hosting.Peers;
using Fracture.Net.Messages;

namespace Fracture.Net.Hosting.Application
{
    public interface IRequest
    {
        #region Properties
        PeerConnection Peer
        {
            get;
        }
        
        byte[] Contents
        {
            get;
        }
        #endregion
    }
    
    public interface IRequest<out T> : IRequest where T : IMessage
    {
        #region Properties
        public T Message
        {
            get;
        }
        #endregion
    }

    public delegate IResponse HandleRequestDelegate(in IRequest request);
    public delegate IResponse HandleRequestDelegate<T>(in IRequest<T> request) where T : IMessage;
}