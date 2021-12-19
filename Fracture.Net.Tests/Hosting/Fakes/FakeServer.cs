using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using Fracture.Common.Events;
using Fracture.Net.Hosting.Servers;
using Fracture.Net.Messages;
using Fracture.Net.Serialization;

namespace Fracture.Net.Tests.Hosting.Fakes
{
    public sealed class FakeServerFrame
    {
        #region Fields
        private readonly Queue<PeerResetEventArgs> leaves;
        private readonly Queue<PeerJoinEventArgs> joins;
        private readonly Queue<PeerMessageEventArgs> messages;
        #endregion
        
        #region Properties
        public IEnumerable<PeerResetEventArgs> Leaves
            => leaves;
        
        public IEnumerable<PeerJoinEventArgs> Joins
            => joins;
        
        public IEnumerable<PeerMessageEventArgs> Messages
            => messages;
        #endregion

        private FakeServerFrame()
        {
            leaves   = new Queue<PeerResetEventArgs>();
            joins    = new Queue<PeerJoinEventArgs>();
            messages = new Queue<PeerMessageEventArgs>();
        }
        
        public FakeServerFrame Leave(PeerConnection peer, ResetReason reason)
        {
            leaves.Enqueue(new PeerResetEventArgs(peer, reason, DateTime.UtcNow.TimeOfDay));
            
            return this;
        }
        
        public FakeServerFrame Join(PeerConnection peer)
        {
            joins.Enqueue(new PeerJoinEventArgs(new PeerConnection(peer.Id, peer.EndPoint), DateTime.UtcNow.TimeOfDay));
            
            return this;
        }
        
        public FakeServerFrame Incoming(PeerConnection peer, byte[] data, int length)
        {
            messages.Enqueue(new PeerMessageEventArgs(peer, data, length, DateTime.UtcNow.TimeOfDay));
            
            return this;
        }
        
        public FakeServerFrame Incoming(PeerConnection peer, IMessage message)
        {
            var size = StructSerializer.GetSizeFromValue(message);
            var data = BufferPool.Take(size);
            
            StructSerializer.Serialize(message, data, 0);
            
            messages.Enqueue(new PeerMessageEventArgs(peer, data, size, DateTime.UtcNow.TimeOfDay));
            
            return this;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FakeServerFrame Create() 
            => new FakeServerFrame();
    } 

    public sealed class FakeServer : IServer
    {
        #region Constant fields
        /// <summary>
        /// Fake port to use with tests.
        /// </summary>
        public const int Port = 2444;
        #endregion
        
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

        private FakeServer(params FakeServerFrame[] frames)
        {
            this.frames = new Queue<FakeServerFrame>(frames);

            peers     = new HashSet<int>();
            leaves    = new HashSet<PeerResetEventArgs>();
            outgoing  = new Queue<ServerMessageEventArgs>();
            endpoints = new Dictionary<int, IPEndPoint>();
        }
        
        public void EnqueueFrame(FakeServerFrame frame)
            => frames.Enqueue(frame ?? throw new ArgumentNullException(nameof(frame)));
        
        public void Disconnect(int id)
            => leaves.Add(new PeerResetEventArgs(new PeerConnection(id, endpoints[id]), ResetReason.LocalReset, DateTime.UtcNow.TimeOfDay));

        public bool Connected(int id)
            => peers.Contains(id);

        public void Send(int id, byte[] data, int offset, int length)
            => outgoing.Enqueue(new ServerMessageEventArgs(new PeerConnection(id, endpoints[id]), data, offset, length, DateTime.UtcNow.TimeOfDay));

        public void Start()
        {
            // Nop.
        }

        public void Shutdown()
        {
            foreach (var peer in peers)
            {
                Reset?.Invoke(this, new PeerResetEventArgs(new PeerConnection(peer, endpoints[peer]), ResetReason.LocalReset, DateTime.UtcNow.TimeOfDay));
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
            foreach (var message in frame?.Messages ?? Array.Empty<PeerMessageEventArgs>())
                Incoming?.Invoke(this, message);
            
            // Handle outgoing.
            while (outgoing.Count != 0)
                Outgoing?.Invoke(this, outgoing.Dequeue());
        }
        
        public void Dispose()
        {
            // Nop.
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FakeServer FromFrames(params FakeServerFrame[] frames) 
            => new FakeServer(frames);
    }
}