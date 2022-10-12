using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using Fracture.Common.Events;
using Fracture.Net.Hosting.Servers;
using Fracture.Net.Messages;
using Fracture.Net.Serialization;

namespace Fracture.Net.Tests.Util.Hosting.Fakes
{
    public delegate FakeServerFrame FakeServerFrameGeneratorDelegate(ulong frame);
    
    public sealed class FakeServerFrame
    {
        #region Fields
        private readonly Queue<PeerResetEventArgs>   leaves;
        private readonly Queue<PeerJoinEventArgs>    joins;
        private readonly Queue<PeerMessageEventArgs> messages;
        #endregion

        #region Properties
        public IEnumerable<PeerResetEventArgs> Leaves => leaves;

        public IEnumerable<PeerJoinEventArgs> Joins => joins;

        public IEnumerable<PeerMessageEventArgs> Messages => messages;

        public ulong Frame
        {
            get;
        }
        #endregion

        private FakeServerFrame(ulong frame)
        {
            Frame    = frame;
            leaves   = new Queue<PeerResetEventArgs>();
            joins    = new Queue<PeerJoinEventArgs>();
            messages = new Queue<PeerMessageEventArgs>();
        }

        public FakeServerFrame Leave(PeerConnection connection, ResetReason reason)
        {
            leaves.Enqueue(new PeerResetEventArgs(connection, reason, DateTime.UtcNow.TimeOfDay));

            return this;
        }

        public FakeServerFrame Join(PeerConnection connection)
        {
            joins.Enqueue(new PeerJoinEventArgs(new PeerConnection(connection.PeerId, connection.EndPoint), DateTime.UtcNow.TimeOfDay));

            return this;
        }

        public FakeServerFrame Incoming(PeerConnection connection, byte[] data, int length)
        {
            messages.Enqueue(new PeerMessageEventArgs(connection, data, length, DateTime.UtcNow.TimeOfDay));

            return this;
        }

        public FakeServerFrame Incoming(PeerConnection connection, in IMessage message)
        {
            var size = StructSerializer.GetSizeFromValue(message);
            var data = BufferPool.Take(size);

            StructSerializer.Serialize(message, data, 0);

            messages.Enqueue(new PeerMessageEventArgs(connection, data, size, DateTime.UtcNow.TimeOfDay));

            return this;
        }

        public FakeServerFrame Noop()
        {
            // In most cases we don't need do this but just to avoid any programming mistakes make sure the frame is clear when it is configured as noop.
            if (leaves.Count + joins.Count + messages.Count > 0)
                throw new InvalidOperationException("Attempting to create noop frame from non-empty one");

            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FakeServerFrame Create(ulong frame)
            => new FakeServerFrame(frame);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<FakeServerFrame> Sequence(ulong startFrame = 0ul, params FakeServerFrameGeneratorDelegate[] generators)
        {
            foreach (var generator in generators)
                yield return generator(startFrame++);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<FakeServerFrame> Sequence(params FakeServerFrameGeneratorDelegate[] generators)
            => Sequence(0ul, generators);
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
        private readonly HashSet<int> peerIds;

        private readonly Dictionary<int, IPEndPoint> endpoints;

        private readonly HashSet<PeerResetEventArgs>   leaves;
        private readonly Queue<ServerMessageEventArgs> outgoing;

        private readonly List<FakeServerFrame> frames;

        private ulong ticks;
        #endregion

        #region Events
        public event StructEventHandler<PeerJoinEventArgs> Join;

        public event StructEventHandler<PeerResetEventArgs> Reset;

        public event StructEventHandler<PeerMessageEventArgs> Incoming;

        public event StructEventHandler<ServerMessageEventArgs> Outgoing;
        #endregion

        public int PeersCount => peerIds.Count;

        public IEnumerable<int> PeerIds => peerIds;

        private FakeServer(params FakeServerFrame[] frames)
        {
            this.frames = new List<FakeServerFrame>(frames ?? throw new ArgumentNullException(nameof(frames)));

            peerIds   = new HashSet<int>();
            leaves    = new HashSet<PeerResetEventArgs>();
            outgoing  = new Queue<ServerMessageEventArgs>();
            endpoints = new Dictionary<int, IPEndPoint>();
        }

        public void EnqueueFrame(FakeServerFrame frame)
        {
            frames.Add(frame ?? throw new ArgumentNullException(nameof(frame)));
            
            frames.Sort((a, b) => a.Frame > b.Frame ? -1 : a.Frame < b.Frame ? 1 : 0);
        }

        public void Disconnect(int peerId)
            => leaves.Add(new PeerResetEventArgs(new PeerConnection(peerId, endpoints[peerId]), ResetReason.LocalReset, DateTime.UtcNow.TimeOfDay));

        public bool Connected(int peerId)
            => peerIds.Contains(peerId);

        public void Send(int peerId, byte[] data, int offset, int length)
            => outgoing.Enqueue(new ServerMessageEventArgs(new PeerConnection(peerId, endpoints[peerId]), data, offset, length, DateTime.UtcNow.TimeOfDay));

        public void Start()
        {
            // Nop.
        }

        public void Shutdown()
        {
            foreach (var peerId in peerIds)
            {
                Reset?.Invoke(this, new PeerResetEventArgs(new PeerConnection(peerId, endpoints[peerId]), ResetReason.LocalReset, DateTime.UtcNow.TimeOfDay));
            }

            peerIds.Clear();
            leaves.Clear();
            endpoints.Clear();
        }

        public void Poll()
        {
            void HandleLeaves(IEnumerable<PeerResetEventArgs> leaves)
            {
                foreach (var leaving in leaves)
                {
                    peerIds.Remove(leaving.Connection.PeerId);
                    endpoints.Remove(leaving.Connection.PeerId);

                    Reset?.Invoke(this, leaving); 
                }
            }
            
            // Handle leaves outside frame context.
            HandleLeaves(leaves);
            
            leaves.Clear();
            
            while (frames.Count != 0 && frames[0].Frame == ticks)
            {
                // Player frames that have been enqueued for this tick.
                var frame = frames[0];
                
                // Handle leaves inside frame context.
                HandleLeaves(frame?.Leaves ?? Array.Empty<PeerResetEventArgs>());
                
                // Handle joins.
                foreach (var joining in frame?.Joins ?? Array.Empty<PeerJoinEventArgs>())
                {
                    peerIds.Add(joining.Connection.PeerId);
                    endpoints.Add(joining.Connection.PeerId, joining.Connection.EndPoint);

                    Join?.Invoke(this, joining);
                }

                // Handle incoming
                foreach (var message in frame?.Messages ?? Array.Empty<PeerMessageEventArgs>())
                    Incoming?.Invoke(this, message);

                frames.RemoveAt(0);
            }

            // Handle outgoing.
            while (outgoing.Count != 0)
                Outgoing?.Invoke(this, outgoing.Dequeue());       
            
            ticks++;
        }

        public void Dispose()
        {
            // Nop.
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FakeServer FromFrames(params FakeServerFrame[] frames)
            => new FakeServer(frames);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FakeServer FromFrames(IEnumerable<FakeServerFrame> frames)
            => new FakeServer(frames.ToArray());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FakeServer Create()
            => new FakeServer();
    }
}