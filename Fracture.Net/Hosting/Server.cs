using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using Fracture.Common.Events;
using Fracture.Net.Hosting.Peers;

namespace Fracture.Net.Hosting
{
    /// <summary>
    /// Structure event arguments used when new connection is made to a listener.
    /// </summary>
    public readonly struct ListenerConnectedEventArgs : IStructEventArgs
    {
        #region Fields
        /// <summary>
        /// Socket of the new connection.
        /// </summary>
        public readonly Socket Socket;
        #endregion
        
        public ListenerConnectedEventArgs(Socket socket)
            => Socket = socket;
    }
    
    /// <summary>
    /// Interface for implementing listeners that provide endpoint for clients to connect to.
    /// </summary>
    public interface IListener : IDisposable
    {
        #region Events
        /// <summary>
        /// Event invoked when new connection is made to the listener.
        /// </summary>
        event StructEventHandler<ListenerConnectedEventArgs> Connected;
        #endregion
        
        /// <summary>
        /// Places the listener to listen state and begins listening for new connections at given port with given backlog size.
        /// </summary>
        /// <param name="port">port the listener will be bound to</param>
        /// <param name="backlog">how many pending clients should be accepted</param>
        void Listen(int port, int backlog);
        
        /// <summary>
        /// Stops listening for any incoming connections and stops accepting them.
        /// </summary>
        void Stop();
        
        /// <summary>
        /// Polls the listener and accepts all incoming connections if the listener is in listening state.
        /// </summary>
        void Poll();
    }
    
    /// <summary>
    /// Interface for implementing low level peer servers.
    /// </summary>
    public interface IServer : IDisposable
    {
        #region Events
        /// <summary>
        /// Event invoked when peer joins the server.
        /// </summary>
        event StructEventHandler<PeerJoinEventArgs> Join;
        
        /// <summary>
        /// Event invoked when peer leaves the server. 
        /// </summary>
        event StructEventHandler<PeerResetEventArgs> Reset;
        
        /// <summary>
        /// Event invoked when server receives message from a peer.
        /// </summary>
        event StructEventHandler<PeerMessageEventArgs> Incoming;
        
        /// <summary>
        /// Event invoked when server is sending message to peer.
        /// </summary>
        event StructEventHandler<ServerMessageEventArgs> Outgoing;
        #endregion
        
        #region Properties
        /// <summary>
        /// Returns count of peers that are currently connected to the server.
        /// </summary>
        int PeersCount
        {
            get;
        }
        
        /// <summary>
        /// Returns id of connected peers.
        /// </summary>
        IEnumerable<int> Peers
        {
            get;
        }
        #endregion

        void Disconnect(int peerId);
        bool Connected(int peerId);
        void Send(int peerId, byte[] data, int offset, int length);
        
        void Start(int port, int backlog);
        void Stop();

        void Poll();
    }
    
    public abstract class Server : IServer
    {
        #region Events
        public event StructEventHandler<PeerJoinEventArgs> Join;
        public event StructEventHandler<PeerResetEventArgs> Reset;
        
        public event StructEventHandler<PeerMessageEventArgs> Incoming;
        public event StructEventHandler<ServerMessageEventArgs> Outgoing;
        #endregion
        
        #region Fields
        private readonly Dictionary<int, IPeer> lookup;
        private readonly List<IPeer> peers;
     
        private readonly IPeerFactory factory;
        private readonly IListener listener;   
        #endregion
        
        #region Properties
        public int PeersCount
            => peers.Count;
        
        public IEnumerable<int> Peers
            => peers.Select(p => p.Id);
        #endregion
        
        protected Server(IPeerFactory factory, IListener listener)
        {
            this.factory  = factory ?? throw new ArgumentNullException(nameof(factory));
            this.listener = listener ?? throw new ArgumentNullException(nameof(listener));
            
            peers  = new List<IPeer>();
            lookup = new Dictionary<int, IPeer>();
        }

        #region Event handlers
        private void Peer_OnReset(object sender, in PeerResetEventArgs e)
        {
            var peer = lookup[e.Peer.Id];
            
            peers.Remove(peer);
            lookup.Remove(e.Peer.Id);
            
            peer.Incoming -= Peer_OnReceived;
            peer.Outgoing -= Peer_OnSending;
            peer.Reset    -= Peer_OnReset;
            
            peer.Dispose();
            
            Reset?.Invoke(this, e);
        }
        
        private void Peer_OnSending(object sender, in ServerMessageEventArgs e)
            => Outgoing?.Invoke(this, e);

        private void Peer_OnReceived(object sender, in PeerMessageEventArgs e)
            => Incoming?.Invoke(this, e);
        
        private void Listener_OnConnected(object sender, in ListenerConnectedEventArgs e)
        {
            var peer = factory.Create(e.Socket);
            
            lookup.Add(peer.Id, peer);
            peers.Add(peer);
            
            Join?.Invoke(this, new PeerJoinEventArgs(new PeerConnection(peer.Id, peer.EndPoint)));
            
            peer.Incoming += Peer_OnReceived;
            peer.Outgoing += Peer_OnSending;
            peer.Reset    += Peer_OnReset;
        }
        #endregion
        
        public void Disconnect(int peerId)
        {
            if (!lookup.TryGetValue(peerId, out var peer))
                throw new InvalidOperationException("peer not connected");
            
            peer.Disconnect();
        }
        
        public bool Connected(int peerId)
            => lookup.TryGetValue(peerId, out var peer) && peer.Connected;

        public void Send(int peerId, byte[] data, int offset, int length)
        {
            if (!lookup.TryGetValue(peerId, out var peer))
                throw new InvalidOperationException("peer not connected");
            
            peer.Send(data, offset, length);
        }
        
        public void Start(int port, int backlog)
        {
            listener.Listen(port, backlog);
            
            listener.Connected += Listener_OnConnected;
            
            listener.Poll();
        }
        
        public void Stop()
        {
            listener.Stop();
            
            for (var i = 0; i < peers.Count; i++)
                peers[i].Disconnect();
            
            while (peers.Count != 0)
            {
                for (var i = 0; i < peers.Count; i++)
                    peers[i].Poll();
            }
        }
        
        public void Poll()
        {
            listener.Poll();
            
            for (var i = 0; i < peers.Count; i++)
                peers[i].Poll();
        }

        public void Dispose()
        {
            listener.Dispose();
            
            foreach (var peer in peers)
                peer.Dispose();
            
            peers.Clear();
            lookup.Clear();
        }
    }
}