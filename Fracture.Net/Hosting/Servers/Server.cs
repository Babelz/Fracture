using System;
using System.Collections.Generic;
using System.Net.Sockets;
using Fracture.Common.Events;

namespace Fracture.Net.Hosting.Servers
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

        public readonly TimeSpan Timestamp;
        #endregion

        public ListenerConnectedEventArgs(Socket socket, in TimeSpan timestamp)
        {
            Socket    = socket ?? throw new ArgumentException(nameof(socket));
            Timestamp = timestamp;
        }
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
        /// Places the listener to listen state and begins listening for new connections.
        /// </summary>
        void Listen();

        /// <summary>
        /// Stops listening for any incoming connections and stops accepting them. This does not dispose the internal socket and listening can be continued
        /// by calling the listen method.
        /// </summary>
        void Deafen();

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
        IEnumerable<int> PeerIds
        {
            get;
        }
        #endregion

        /// <summary>
        /// Disconnects peer with given id.
        /// </summary>
        void Disconnect(int peerId);

        /// <summary>
        /// Sends message to peer with given id.
        /// </summary>
        void Send(int peerId, byte[] data, int offset, int length);

        void Start();

        void Shutdown();

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
        private readonly List<IPeer>            peerIds;

        private readonly IPeerFactory factory;
        private readonly IListener    listener;
        #endregion

        #region Properties
        public int PeersCount => peerIds.Count;

        public IEnumerable<int> PeerIds => lookup.Keys;
        #endregion

        protected Server(IPeerFactory factory, IListener listener)
        {
            this.factory  = factory ?? throw new ArgumentNullException(nameof(factory));
            this.listener = listener ?? throw new ArgumentNullException(nameof(listener));

            peerIds = new List<IPeer>();
            lookup  = new Dictionary<int, IPeer>();
        }

        #region Event handlers
        private void Peer_OnReset(object sender, in PeerResetEventArgs e)
        {
            var peer = lookup[e.Connection.PeerId];

            peerIds.Remove(peer);
            lookup.Remove(e.Connection.PeerId);

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
            // Create peer state in the server and listen for events.
            var peer = factory.Create(e.Socket);

            lookup.Add(peer.Id, peer);
            peerIds.Add(peer);

            peer.Incoming += Peer_OnReceived;
            peer.Outgoing += Peer_OnSending;
            peer.Reset    += Peer_OnReset;

            // Invoke joining.
            Join?.Invoke(this, new PeerJoinEventArgs(new PeerConnection(peer.Id, peer.EndPoint), e.Timestamp));
        }
        #endregion

        public void Disconnect(int peerId)
        {
            if (!lookup.TryGetValue(peerId, out var peer))
                throw new InvalidOperationException("peer not connected");

            peer.Disconnect();
        }

        public void Send(int peerId, byte[] data, int offset, int length)
        {
            if (!lookup.TryGetValue(peerId, out var peer))
                throw new InvalidOperationException("peer not connected");

            peer.Send(data, offset, length);
        }

        public void Start()
        {
            listener.Listen();

            listener.Connected += Listener_OnConnected;

            listener.Poll();
        }

        public void Shutdown()
        {
            listener.Deafen();

            for (var i = 0; i < peerIds.Count; i++)
                peerIds[i].Disconnect();

            while (peerIds.Count != 0)
            {
                for (var i = 0; i < peerIds.Count; i++)
                    peerIds[i].Poll();
            }
        }

        public void Poll()
        {
            listener.Poll();

            for (var i = 0; i < peerIds.Count; i++)
                peerIds[i].Poll();
        }

        public void Dispose()
        {
            listener.Dispose();

            foreach (var peerId in peerIds)
                peerId.Dispose();

            peerIds.Clear();
            lookup.Clear();
        }
    }
}