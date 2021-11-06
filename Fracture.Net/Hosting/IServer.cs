using System;
using System.Collections.Generic;
using System.Linq;
using Fracture.Common.Events;
using Fracture.Net.Hosting.Peers;

namespace Fracture.Net.Hosting
{
    public interface IServer : IDisposable
    {
        #region Events
        event StructEventHandler<PeerJoinEventArgs> Join;
        event StructEventHandler<PeerResetEventArgs> Reset;
        
        event StructEventHandler<PeerMessageEventArgs> Incoming;
        event StructEventHandler<ServerMessageEventArgs> Outgoing;
        #endregion
        
        #region Properties
        int PeersCount
        {
            get;
        }
        
        IEnumerable<int> Peers
        {
            get;
        }
        #endregion

        void Disconnect(int peerId);
        bool IsConnected(int peerId);
        
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
        private void PeerOnReset(object sender, in PeerResetEventArgs e)
        {
            var peer = lookup[e.Peer.Id];
            
            peers.Remove(peer);
            lookup.Remove(e.Peer.Id);
            
            peer.Incoming     -= Peer_OnReceived;
            peer.Outgoing     -= Peer_OnSending;
            peer.Reset -= PeerOnReset;
            
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
            peer.Reset    += PeerOnReset;
        }
        #endregion
        
        public void Disconnect(int peerId)
        {
            if (!lookup.TryGetValue(peerId, out var peer))
                throw new InvalidOperationException("peer not connected");
            
            peer.Disconnect();
        }
        
        public bool IsConnected(int peerId)
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