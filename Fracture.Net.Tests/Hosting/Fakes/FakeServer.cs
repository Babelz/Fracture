using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Dia2Lib;
using Fracture.Common.Events;
using Fracture.Net.Hosting.Peers;
using Fracture.Net.Hosting.Servers;

namespace Fracture.Net.Tests.Hosting.Fakes
{
    public sealed class FakeServerFrame
    {
        #region Properties
        public IEnumerable<PeerResetEventArgs> Leaves
        {
            get;
        }
        
        public IEnumerable<PeerJoinEventArgs> Joins
        {
            get;
        }
        
        public IEnumerable<PeerMessageEventArgs> Incoming
        {
            get;
        }
        #endregion
        
        public FakeServerFrame(IEnumerable<PeerResetEventArgs> leaves = null,
                               IEnumerable<PeerJoinEventArgs> joins = null,
                               IEnumerable<PeerMessageEventArgs> incoming = null)
        {
            Leaves   = leaves;
            Joins    = joins;
            Incoming = incoming;
        }
    }

    public sealed class FakeServer : IServer
    {
        #region Fields
        private readonly HashSet<int> peers;
        
        private readonly Dictionary<int, IPEndPoint> endpoints; 
        
        private readonly HashSet<PeerResetEventArgs> leaves;
        private readonly Queue<ServerMessageEventArgs> outgoing;
        
        private readonly Queue<FakeServerFrame> frames;
        #endregion
        
        #region Events
        public event StructEventHandler<PeerJoinEventArgs> Join;
        public event StructEventHandler<PeerResetEventArgs> Reset;
        
        public event StructEventHandler<PeerMessageEventArgs> Incoming;
        public event StructEventHandler<ServerMessageEventArgs> Outgoing;
        #endregion
        
        public int PeersCount
            => peers.Count;

        public IEnumerable<int> Peers
            => peers;

        public FakeServer()
        {
            peers  = new HashSet<int>();
            frames = new Queue<FakeServerFrame>();
         
            leaves   = new HashSet<PeerResetEventArgs>();
            outgoing = new Queue<ServerMessageEventArgs>();
            
            endpoints = new Dictionary<int, IPEndPoint>();
        }
        
        public void EnqueueFrame(FakeServerFrame frame)
            => frames.Enqueue(frame ?? throw new ArgumentNullException(nameof(frame)));
        
        public void Disconnect(int id)
            => leaves.Add(new PeerResetEventArgs(new PeerConnection(id, endpoints[id]), 
                                                 PeerResetReason.ServerReset, 
                                                 DateTime.UtcNow.TimeOfDay));

        public bool Connected(int id)
            => peers.Contains(id);

        public void Send(int id, byte[] data, int offset, int length)
            => outgoing.Enqueue(new ServerMessageEventArgs(new PeerConnection(id, endpoints[id]), 
                                                           data, 
                                                           offset, 
                                                           length, 
                                                           DateTime.UtcNow.TimeOfDay));

        public void Start(int port, int backlog)
        {
            // Nop.
        }

        public void Shutdown()
        {
            foreach (var peer in peers)
            {
                Reset?.Invoke(this, new PeerResetEventArgs(new PeerConnection(peer, endpoints[peer]), 
                                                           PeerResetReason.ServerReset, 
                                                           DateTime.UtcNow.TimeOfDay));
            }
            
            peers.Clear();
            leaves.Clear();
            endpoints.Clear();
        }

        public void Poll()
        {
            // Get frame.
            var frame = frames.Count != 0 ? frames.Dequeue() : null;
            
            // Handle leaves.
            foreach (var leave in leaves.Concat(frame?.Leaves ?? Array.Empty<PeerResetEventArgs>()))
            {
                peers.Remove(leave.Peer.Id);
                endpoints.Remove(leave.Peer.Id);

                Reset?.Invoke(this, leave);
            }
            
            leaves.Clear();
            
            // Handle joins.
            foreach (var join in frame?.Joins ?? Array.Empty<PeerJoinEventArgs>())
            {
                peers.Add(join.Peer.Id);
                endpoints.Add(join.Peer.Id, join.Peer.EndPoint);

                Join?.Invoke(this, join);
            }
            
            // Handle incoming
            foreach (var incoming in frame?.Incoming ?? Array.Empty<PeerMessageEventArgs>())
                Incoming?.Invoke(this, incoming);
            
            // Handle outgoing.
            while (outgoing.Count != 0)
                Outgoing?.Invoke(this, outgoing.Dequeue());
        }
        
        public void Dispose()
        {
            // Nop.
        }
    }
}