using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using Fracture.Common.Di.Attributes;
using Fracture.Engine.Core;
using Fracture.Net;
using Fracture.Net.Clients;
using Fracture.Net.Messages;

namespace Fracture.Engine.Net
{
    public delegate void NetSystemConnectedCallback(IPEndPoint endPoint);
    public delegate void NetSystemConnectFailedCallback(Exception exception, SocketError error);
    public delegate void NetSystemDisconnectedCallback(Exception exception, ResetReason reason);

    /// <summary>
    /// Interface for implementing low level networking systems that provide functionality for connection management.
    /// </summary>
    public interface INetConnectionSystem : IGameEngineSystem
    {
        #region Properties
        /// <summary>
        /// Gets boolean declaring whether the system is connected to the remote host.
        /// </summary>
        public bool IsConnected
        {
            get;
        }
        #endregion
        
        /// <summary>
        /// Begins connecting to the remote host at given endpoint. The provided callbacks are called when an event
        /// regarding the connection status occurs.
        /// </summary>
        /// <param name="endPoint">endpoint where the connection should be established</param>
        /// <param name="connectedCallback">callback invoked when connection has been established</param>
        /// <param name="connectFailedCallback">callback invoked when connecting to the remote endpoint failed</param>
        /// <param name="disconnectedCallback">callback invoked when the underlying connection has been disconnected</param>
        void Connect(IPEndPoint endPoint,
                     NetSystemConnectedCallback connectedCallback,
                     NetSystemConnectFailedCallback connectFailedCallback,
                     NetSystemDisconnectedCallback disconnectedCallback);
        
        /// <summary>
        /// Begins disconnecting the underlying connection with the remote host.
        /// </summary>
        void Disconnect();
    }
    
    public delegate void NetMessageQueryResponseCallback(IQueryMessage response);
    public delegate void NetMessageQueryTimeoutCallback(IQueryMessage request);
    
    /// <summary>
    /// Interface for implementing net systems that provide messaging functionality.
    /// </summary>
    public interface INetMessageSystem : INetConnectionSystem, INetPacketHandler
    {
        /// <summary>
        /// Sends message to the connected remote endpoint.
        /// </summary>
        void Send<T>(in T message) where T : IMessage;
        
        /// <summary>
        /// Sends query message to connected remote end point. Based on the query outcome the provided callbacks will be invoked.
        /// </summary>
        /// <param name="message">query message that will be send out</param>
        /// <param name="responseCallback">callback invoked when response to the query has been received</param>
        /// <param name="timeoutCallback">callback invoked in case the query has timed out</param>
        void Send<T>(in T message, 
                     NetMessageQueryResponseCallback responseCallback, 
                     NetMessageQueryTimeoutCallback timeoutCallback) where T : IQueryMessage;
    }
    
    /// <summary>
    /// Default implementation of <see cref="INetMessageSystem"/>.
    /// </summary>
    public sealed class NetSystem : GameEngineSystem, INetMessageSystem
    {
        #region Private query message context class
        public class QueryMessageContext
        {
            #region Properties
            public IQueryMessage Request
            {
                get;
            }

            public NetMessageQueryResponseCallback ResponseCallback
            {
                get;
            }

            public NetMessageQueryTimeoutCallback TimeoutCallback
            {
                get;
            }
        
            public TimeSpan Timestamp
            {
                get;
            }
            #endregion

            public QueryMessageContext(IQueryMessage request, NetMessageQueryResponseCallback responseCallback, NetMessageQueryTimeoutCallback timeoutCallback)
            {
                Request          = request;
                ResponseCallback = responseCallback;
                TimeoutCallback  = timeoutCallback;
                Timestamp        = DateTime.UtcNow.TimeOfDay;
            }
        }
        #endregion

        #region Static fields
        public static readonly TimeSpan DefaultQueryGracePeriod = TimeSpan.FromSeconds(15);
        #endregion
        
        #region Fields
        private readonly TimeSpan queryGracePeriod;
        private readonly Client client;
        
        private NetSystemConnectedCallback connectedCallback;
        private NetSystemConnectFailedCallback connectFailedCallback;
        private NetSystemDisconnectedCallback disconnectedCallback;
        
        private readonly NetPacketHandler packetHandler;
        
        private readonly List<QueryMessageContext> queries;
        #endregion

        #region Properties
        public bool IsConnected
            => client.IsConnected;
        #endregion

        public NetSystem(Client client, TimeSpan queryGracePeriod)
        {
            this.client           = client ?? throw new ArgumentException(nameof(client));
            this.queryGracePeriod = queryGracePeriod;
            
            packetHandler = new NetPacketHandler();
            queries       = new List<QueryMessageContext>(256);
        }

        public NetSystem(Client client)   
            : this(client, DefaultQueryGracePeriod)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReleasePacket(ClientUpdate.Packet packet)
        {
            Message.Release(packet.Message);
            
            BufferPool.Return(packet.Contents);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReleaseQueryContext(QueryMessageContext context)
        {
            Message.Release(context.Request);
        }
        
        private void HandleClientUpdates()
        {
            foreach (var update in client.Poll())
            {
                switch (update)
                {
                    case ClientUpdate.Connected cu:
                        connectedCallback?.Invoke(cu.RemoteEndPoint);
                        break;
                    case ClientUpdate.Disconnected du:
                        disconnectedCallback?.Invoke(du.Exception, du.Reason);
                        break;
                    case ClientUpdate.ConnectFailed cf:
                        connectFailedCallback?.Invoke(cf.Exception, cf.Error);
                        break;
                    case ClientUpdate.Packet packet:
                        if (packet.Origin == ClientUpdate.PacketOrigin.Remote)
                            HandlePacket(packet);
                        else
                            ReleasePacket(packet);
                        break;
                }
            }
        }

        private void UpdateQueryContexts()
        {
            var now = DateTime.UtcNow.TimeOfDay;
            var i   = 0;
            
            while (i < queries.Count)
            {
                var context = queries[i];
                
                if ((now - context.Timestamp) >= queryGracePeriod)
                {
                    context.TimeoutCallback?.Invoke(context.Request);
                    
                    queries.RemoveAt(i);
                    
                    ReleaseQueryContext(context);
                    
                    continue;
                }
                
                i++;
            }
        }

        private void HandlePacket(ClientUpdate.Packet packet)
        {
            if (queries.Any() && packet.Message is IQueryMessage query)
            {
                var context = queries.FirstOrDefault(c => c.Request.QueryId == query.QueryId);
                
                if (context != null)
                {
                    context.ResponseCallback?.Invoke(query);
                    
                    queries.Remove(context);
                    
                    ReleaseQueryContext(context);
                }
            }
            
            packetHandler.Handle(packet);
            
            ReleasePacket(packet);
        }
        
        public void Connect(IPEndPoint endPoint,
                            NetSystemConnectedCallback connectedCallback,
                            NetSystemConnectFailedCallback connectFailedCallback,
                            NetSystemDisconnectedCallback disconnectedCallback)
        {
            this.connectedCallback     = connectedCallback ?? throw new ArgumentNullException(nameof(connectedCallback)); 
            this.connectFailedCallback = connectFailedCallback ?? throw new ArgumentNullException(nameof(connectFailedCallback));
            this.disconnectedCallback  = disconnectedCallback ?? throw new ArgumentNullException(nameof(disconnectedCallback));
            
            client.Connect(endPoint);
        }
        
        public void Disconnect()
            => client.Disconnect();
        
        public void Use(string name, NetPacketMatchDelegate match, NetPacketHandlerDelegate handler) 
            => packetHandler.Use(name, match, handler);

        public void Revoke(string name)
            => packetHandler.Revoke(name);

        public void Send<T>(in T message) where T : IMessage
            => client.Send(message);

        public void Send<T>(in T message,
                            NetMessageQueryResponseCallback responseCallback,
                            NetMessageQueryTimeoutCallback timeoutCallback) where T : IQueryMessage
        {
            var context = new QueryMessageContext(message,
                                                  responseCallback,
                                                  timeoutCallback);
            queries.Add(context);
            
            client.Send(message);
        }
        
        public override void Update(IGameEngineTime time)
        {
            HandleClientUpdates();
            
            UpdateQueryContexts();
        }
    }
}