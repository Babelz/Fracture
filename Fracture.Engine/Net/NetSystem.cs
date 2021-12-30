using System;
using System.Net;
using System.Net.Sockets;
using Fracture.Common.Memory.Pools;
using Fracture.Engine.Core;
using Fracture.Net;
using Fracture.Net.Messages;

namespace Fracture.Engine.Net
{
    public delegate void NetSystemConnectedCallback(IPEndPoint endPoint);
    public delegate void NetSystemConnectFailedCallback(Exception exception, SocketError error);
    public delegate void NetSystemDisconnectedCallback(Exception exception, ResetReason reason);

    public interface INetSystem : IGameEngineSystem
    {
        #region Properties
        public bool Connected
        {
            get;
        }
        #endregion
        
        void Connect(IPEndPoint endPoint,
                     NetSystemConnectedCallback connectedCallback,
                     NetSystemConnectFailedCallback connectFailedCallback,
                     NetSystemDisconnectedCallback disconnectedCallback);
        
        void Disconnect();
    }
    
    public delegate void NetMessageQueryResponseCallback(IQueryMessage request, IQueryMessage response);
    public delegate void NetMessageQueryTimeoutCallback(IQueryMessage request);
    
    public interface INetMessageSystem : INetSystem, INetMessageHandler
    {
        void Message<T>(PoolElementDecoratorDelegate<T> decorator) where T : IMessage;
        
        void Query<T>(PoolElementDecoratorDelegate<T> decorator, 
                      NetMessageQueryResponseCallback responseCallback, 
                      NetMessageQueryTimeoutCallback timeoutCallback) where T : IQueryMessage;
    }

    public sealed class NetSystem
    {
    }
}