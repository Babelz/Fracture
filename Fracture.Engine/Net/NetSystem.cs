using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using Fracture.Common.Collections;
using Fracture.Common.Memory.Pools;
using Fracture.Engine.Core;
using Fracture.Net;
using Fracture.Net.Clients;
using Fracture.Net.Messages;

namespace Fracture.Engine.Net
{
    public delegate void NetSystemConnectedCallback(IPEndPoint endPoint);
    public delegate void NetSystemConnectFailedCallback(Exception exception, SocketError error);
    public delegate void NetSystemDisconnectedCallback(Exception exception, ResetReason reason);

    public interface INetSystem : IGameEngineSystem
    {
        #region Properties
        public bool IsConnected
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
    
    public delegate void NetMessageQueryResponseCallback(IQueryMessage response);
    public delegate void NetMessageQueryTimeoutCallback(IQueryMessage request);
    
    public interface INetSystemPacketRouter : INetPacketHandler
    {
        #region Properties
        object Consumer
        {
            get;
        }
        #endregion
    }
    
    public sealed class NetSystemPacketRouter : INetSystemPacketRouter
    {
        #region Fields
        private readonly IManagedNetPacketHandler managedHandler;
        #endregion

        #region Properties
        public object Consumer
        {
            get;
        }
        #endregion
        
        public NetSystemPacketRouter(object consumer, IManagedNetPacketHandler managedHandler)
        {    
            this.managedHandler = managedHandler ?? throw new ArgumentNullException(nameof(managedHandler));

            Consumer = consumer ?? throw new ArgumentNullException(nameof(consumer));
        }
        
        public void Use(NetPacketMatchDelegate match, NetPacketHandlerDelegate handler)
            => managedHandler.Use(Consumer, match, handler);
    }
    
    public interface INetPacketSystem : INetSystem
    {
        void Send<T>(PoolElementDecoratorDelegate<T> decorator) where T : class, IMessage, new();
        
        void Send<T>(PoolElementDecoratorDelegate<T> decorator, 
                     NetMessageQueryResponseCallback responseCallback, 
                     NetMessageQueryTimeoutCallback timeoutCallback) where T : class, IQueryMessage, new();
        
        INetSystemPacketRouter Create(object consumer);
        void Delete(INetSystemPacketRouter router);
    }
    
    public sealed class NetSystem : GameEngineSystem, INetPacketSystem
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
        
        private readonly ManagedNetPacketHandler managedPacketHandler;
        
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
            
            managedPacketHandler = new ManagedNetPacketHandler();
            queries              = new List<QueryMessageContext>(256);
        }

        public NetSystem(Client client)   
            : this(client, DefaultQueryGracePeriod)
        {
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
            
            managedPacketHandler.Handle(packet);
            
            ReleasePacket(packet);
        }
        
        public void Disconnect()
            => client.Disconnect();
        
        public INetSystemPacketRouter Create(object consumer)
            => new NetSystemPacketRouter(consumer, managedPacketHandler);
        
        public void Delete(INetSystemPacketRouter router)
        {
            if (!managedPacketHandler.Clear(router.Consumer))
                throw new InvalidOperationException($"attempt to delete router that does not belong to {nameof(NetSystem)} was made");
        }

        public void Send<T>(PoolElementDecoratorDelegate<T> decorator) where T : class, IMessage, new()
            => client.Send(Message.Create(decorator));

        public void Send<T>(PoolElementDecoratorDelegate<T> decorator,
                            NetMessageQueryResponseCallback responseCallback,
                            NetMessageQueryTimeoutCallback timeoutCallback) where T : class, IQueryMessage, new()
        {
            var request = Message.Create(decorator);
            var context = new QueryMessageContext(request,
                                                  responseCallback,
                                                  timeoutCallback);
            queries.Add(context);
            
            client.Send(request);
        }
        
        public override void Update(IGameEngineTime time)
        {
            HandleClientUpdates();
            
            UpdateQueryContexts();
        }
    }
}