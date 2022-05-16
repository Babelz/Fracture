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
        public IEnumerable<PeerResetEventArgs> Leaves => leaves;

        public IEnumerable<PeerJoinEventArgs> Joins => joins;

        public IEnumerable<PeerMessageEventArgs> Messages => messages;
        #endregion

        private FakeServerFrame()
        {
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

        public FakeServerFrame Incoming(PeerConnection connection, byte [] data, int length)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FakeServerFrame Create() => new FakeServerFrame();
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

        public int PeersCount => peerIds.Count;

        public IEnumerable<int> PeerIds => peerIds;

        private FakeServer(params FakeServerFrame [] frames)
        {
            this.frames = new Queue<FakeServerFrame>(frames);

            peerIds   = new HashSet<int>();
            leaves    = new HashSet<PeerResetEventArgs>();
            outgoing  = new Queue<ServerMessageEventArgs>();
            endpoints = new Dictionary<int, IPEndPoint>();
        }

        public void EnqueueFrame(FakeServerFrame frame) => frames.Enqueue(frame ?? throw new ArgumentNullException(nameof(frame)));

        public void Disconnect(int peerId) =>
            leaves.Add(new PeerResetEventArgs(new PeerConnection(peerId, endpoints[peerId]), ResetReason.LocalReset, DateTime.UtcNow.TimeOfDay));

        public bool Connected(int peerId) => peerIds.Contains(peerId);

        public void Send(int peerId, byte [] data, int offset, int length) =>
            outgoing.Enqueue(new ServerMessageEventArgs(new PeerConnection(peerId, endpoints[peerId]), data, offset, length, DateTime.UtcNow.TimeOfDay));

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
            // Get frame.
            var frame = frames.Count != 0 ? frames.Dequeue() : null;

            // Handle leaves.
            foreach (var leaving in leaves.Concat(frame?.Leaves ?? Array.Empty<PeerResetEventArgs>()))
            {
                peerIds.Remove(leaving.Connection.PeerId);
                endpoints.Remove(leaving.Connection.PeerId);

                Reset?.Invoke(this, leaving);
            }

            leaves.Clear();

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

            // Handle outgoing.
            while (outgoing.Count != 0)
                Outgoing?.Invoke(this, outgoing.Dequeue());
        }

        public void Dispose()
        {
            // Nop.
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FakeServer FromFrames(params FakeServerFrame [] frames) => new FakeServer(frames);
    }
}