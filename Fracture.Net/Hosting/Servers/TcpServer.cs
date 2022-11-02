using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using Fracture.Common.Collections.Concurrent;
using Fracture.Common.Events;

namespace Fracture.Net.Hosting.Servers
{
    /// <summary>
    /// Peer implementation that uses the TCP protocol for communication.
    /// </summary>
    public sealed class TcpPeer : IPeer
    {
        #region Constant fields
        private const int ReceiveBufferSize = 65536;
        #endregion

        #region Static fields
        private static readonly HashSet<SocketError> GracefulSocketErrors = new HashSet<SocketError>
        {
            SocketError.Shutdown,
            SocketError.ConnectionReset,
            SocketError.NetworkReset,
            SocketError.NotConnected,
        };

        private static int IdCounter;
        #endregion

        #region Fields
        private readonly TimeSpan gracePeriod;

        private readonly byte[] receiveBuffer;
        private readonly Socket socket;

        private readonly LockedDoubleBuffer<PeerMessageEventArgs>   incomingMessageBuffer;
        private readonly LockedDoubleBuffer<ServerMessageEventArgs> outgoingMessageBuffer;

        private IAsyncResult receiveResult;
        private IAsyncResult disconnectResult;

        private DateTime lastReceiveTime;

        private ResetReason reason;
        private PeerState   state;
        #endregion

        #region Events
        public event StructEventHandler<PeerResetEventArgs> Reset;

        public event StructEventHandler<PeerMessageEventArgs> Incoming;

        public event StructEventHandler<ServerMessageEventArgs> Outgoing;
        #endregion

        #region Properties
        private bool IsConnected => socket.Connected;

        private bool HasTimedOut => DateTime.UtcNow - lastReceiveTime > gracePeriod;

        private bool IsReceiving => !receiveResult?.IsCompleted ?? false;

        private bool HasDisconnected => disconnectResult?.IsCompleted ?? false;

        public int Id
        {
            get;
        }

        public IPEndPoint EndPoint
        {
            get;
        }
        #endregion

        public TcpPeer(Socket socket, TimeSpan gracePeriod)
        {
            this.socket      = socket;
            this.gracePeriod = gracePeriod;

            Id       = IdCounter++;
            EndPoint = (IPEndPoint)socket.RemoteEndPoint;

            incomingMessageBuffer = new LockedDoubleBuffer<PeerMessageEventArgs>();
            outgoingMessageBuffer = new LockedDoubleBuffer<ServerMessageEventArgs>();
            receiveBuffer         = new byte[ReceiveBufferSize];

            state           = PeerState.Connected;
            lastReceiveTime = DateTime.UtcNow;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void HandleSocketException(Exception ex)
        {
            if (ex is SocketException sex)
            {
                if (!GracefulSocketErrors.Contains(sex.SocketErrorCode))
                    throw ex;
            }
            else if (!(ex is ObjectDisposedException))
                throw ex;
        }

        #region Async callbacks
        private void DisconnectCallback(IAsyncResult ar)
        {
            // Handle all async operations in try catch because the socket might already be closed or disposed when the callback is executed.
            try
            {
                socket.EndDisconnect(ar);
            }
            catch (Exception e)
            {
                HandleSocketException(e);
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            // Handle all async operations in try catch because the socket might already be closed or disposed when the callback is executed.
            try
            {
                socket.EndSend(ar);

                outgoingMessageBuffer.Push((ServerMessageEventArgs)ar.AsyncState);
            }
            catch (Exception e)
            {
                HandleSocketException(e);
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            // Handle all async operations in try catch because the socket might already be closed or disposed when the callback is executed.
            try
            {
                var length = socket.EndReceive(ar);

                if (length == 0)
                    return;

                var contents = BufferPool.Take(length);

                Array.Copy(receiveBuffer, 0, contents, 0, length);

                incomingMessageBuffer.Push(new PeerMessageEventArgs(new PeerConnection(Id, EndPoint), contents, length, DateTime.UtcNow.TimeOfDay));
            }
            catch (Exception e)
            {
                HandleSocketException(e);
            }
        }
        #endregion

        private void InternalDisconnect(ResetReason reason)
        {
            // Omit calls if peer is not in connected state.
            if (state != PeerState.Connected)
                return;

            try
            {
                disconnectResult = socket.BeginDisconnect(false, DisconnectCallback, null);
                state            = PeerState.Disconnecting;
            }
            catch (Exception e)
            {
                HandleSocketException(e);

                state = PeerState.Disconnected;
            }

            this.reason = reason;
        }

        private void HandleMessages()
        {
            // Get all messages from both of the locked double buffers. 
            var outgoingMessages = outgoingMessageBuffer.Read();
            var incomingMessages = incomingMessageBuffer.Read();

            // Handle all outgoing messages.
            foreach (var outgoing in outgoingMessages)
                Outgoing?.Invoke(this, outgoing);

            // Handle all incoming messages.
            foreach (var incoming in incomingMessages)
                Incoming?.Invoke(this, incoming);

            // Set activity timestamp to be current time if we are sending or receiving any messages.
            lastReceiveTime = incomingMessages.Length > 0 ? DateTime.UtcNow : lastReceiveTime;
        }

        private void UpdateConnectedState()
        {
            if (state != PeerState.Connected)
                return;

            if (!IsConnected)
                InternalDisconnect(ResetReason.RemoteReset);
            else if (HasTimedOut)
                InternalDisconnect(ResetReason.TimedOut);
            else if (!IsReceiving)
            {
                try
                {
                    receiveResult = socket.BeginReceive(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, ReceiveCallback, null);
                }
                catch (Exception e)
                {
                    HandleSocketException(e);

                    InternalDisconnect(ResetReason.RemoteReset);
                }
            }
        }

        private void UpdateDisconnectingState()
        {
            if (state != PeerState.Disconnecting)
                return;

            if (!HasDisconnected)
                return;

            state = PeerState.Disconnected;

            Reset?.Invoke(this, new PeerResetEventArgs(new PeerConnection(Id, EndPoint), reason, DateTime.UtcNow.TimeOfDay));
        }

        public void Disconnect()
            => InternalDisconnect(ResetReason.LocalReset);

        public void Send(byte[] data, int offset, int length)
        {
            if (state != PeerState.Connected)
                return;

            if (!IsConnected)
                return;

            try
            {
                socket.BeginSend(data,
                                 offset,
                                 length,
                                 SocketFlags.None,
                                 SendCallback,
                                 new ServerMessageEventArgs(new PeerConnection(Id, EndPoint), data, offset, length, DateTime.UtcNow.TimeOfDay));
            }
            catch (Exception e)
            {
                HandleSocketException(e);

                InternalDisconnect(ResetReason.RemoteReset);
            }
        }

        public void Poll()
        {
            if (state == PeerState.Disconnected)
                return;

            HandleMessages();

            UpdateConnectedState();

            UpdateDisconnectingState();
        }

        public void Dispose()
            => socket.Dispose();
    }

    /// <summary>
    /// Peer factory implementation that creates peers that use the TCP protocol for communication.
    /// </summary>
    public sealed class TcpPeerFactory : IPeerFactory
    {
        #region Constant fields
        public const int BufferSizes = 65536;
        #endregion

        #region Fields
        private readonly TimeSpan gracePeriod;

        private readonly int bufferSizes;
        #endregion

        public TcpPeerFactory(TimeSpan gracePeriod, int bufferSizes = BufferSizes)
        {
            this.gracePeriod = gracePeriod;
            this.bufferSizes = bufferSizes;
        }

        public IPeer Create(Socket socket)
        {
            if (socket.ProtocolType != ProtocolType.Tcp)
                throw new ArgumentException("expecting TCP socket");

            socket.NoDelay           = true;
            socket.ReceiveBufferSize = bufferSizes;
            socket.SendBufferSize    = bufferSizes;

            return new TcpPeer(socket, gracePeriod);
        }
    }

    /// <summary>
    /// Listener implementation that listens for incoming TPC connections.
    /// </summary>
    public sealed class TcpListener : IListener
    {
        #region Fields
        private readonly int port;
        private readonly int backlog;

        private readonly LockedDoubleBuffer<Socket> newConnections;
        private readonly Socket                     listener;

        private IAsyncResult listenResult;

        private long acceptingData;
        #endregion

        #region Events
        public event StructEventHandler<ListenerConnectedEventArgs> Connected;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets in thread safe manner the accepting state of the listener.
        /// </summary>
        private bool Accepting
        {
            get => Convert.ToBoolean(Interlocked.CompareExchange(ref acceptingData, 0, 0));
            set => Interlocked.Exchange(ref acceptingData, Convert.ToInt32(value));
        }

        /// <summary>
        /// Returns boolean declaring whether the listener is listening for incoming connections.
        /// </summary>
        private bool Listening => !listenResult?.IsCompleted ?? false;
        #endregion

        public TcpListener(int port, int backlog = 0)
        {
            this.port    = port;
            this.backlog = backlog;

            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true,
            };

            newConnections = new LockedDoubleBuffer<Socket>();
        }

        #region Async callbacks
        /// <summary>
        /// Callback that accepts incoming clients and buffers the for the <see cref="Poll"/> method to handle when called.
        /// </summary>
        private void AcceptCallback(IAsyncResult ar)
        {
            // Handle all async operations in try catch because the listener might already be closed or disposed when the callback is executed.
            try
            {
                var socket = listener.EndAccept(ar);

                // If we are not accepting clients anymore just dispose any new sockets if possible.
                if (!Accepting)
                {
                    socket.Disconnect(false);

                    socket.Dispose();

                    return;
                }

                newConnections.Push(socket);
            }
            catch (SocketException se)
            {
                // Only expect the socket to be shutdown if an exception occurs.
                if (se.SocketErrorCode != SocketError.Shutdown)
                    throw;
            }
        }
        #endregion

        public void Listen()
        {
            if (Accepting)
                throw new InvalidOperationException("listener already listening");

            Accepting = true;

            // Place socket in listening state and listen on all interfaces on given port.
            listener.Bind(new IPEndPoint(IPAddress.Any, port));

            listener.Listen(backlog);
        }

        public void Deafen()
        {
            if (!Accepting)
                throw new InvalidOperationException("listener not in listen state");

            Accepting = false;

            // Stop listening and allow socket reuse when listen is called again.
            listener.Shutdown(SocketShutdown.Both);

            listener.Disconnect(true);
        }

        private void UpdateListenState()
        {
            // Start listening async if accepting new connections and we are not already listening.
            if (Accepting && !Listening)
                listenResult = listener.BeginAccept(AcceptCallback, null);
        }

        private void HandleNewConnections()
        {
            // Invoke events for each new connection.
            foreach (var newConnection in newConnections.Read())
                Connected?.Invoke(this, new ListenerConnectedEventArgs(newConnection, DateTime.UtcNow.TimeOfDay));
        }

        public void Poll()
        {
            UpdateListenState();

            HandleNewConnections();
        }

        public void Dispose()
            => listener.Dispose();
    }

    /// <summary>
    /// Class that provides TCP server functionality using <see cref="TcpListener"/> and <see cref="TcpPeerFactory"/>.
    /// </summary>
    public sealed class TcpServer : Server
    {
        public TcpServer(TimeSpan gracePeriod, int port, int backlog = 0)
            : base(new TcpPeerFactory(gracePeriod), new TcpListener(port, backlog))
        {
        }
    }
}