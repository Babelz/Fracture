using System;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using Fracture.Common.Collections;
using Fracture.Common.Collections.Concurrent;
using Fracture.Common.Events;
using Fracture.Common.Memory.Pools;

namespace Fracture.Net.Hosting
{
    /// <summary>
    /// Structure that represents peer connection.
    /// </summary>
    public readonly struct PeerConnection
    {
        #region Fields
        /// <summary>
        /// Runtime id of the peer.
        /// </summary>
        public readonly int Id;
        
        /// <summary>
        /// Remote end point of the peer.
        /// </summary>
        public readonly IPEndPoint EndPoint;
        #endregion
        
        public PeerConnection(int id, IPEndPoint endPoint)
        {
            Id       = id;
            EndPoint = endPoint;
        }
    }
    
    /// <summary>
    /// Interface for implementing peer factories.
    /// </summary>
    public interface IPeerFactory
    {
        /// <summary>
        /// Creates new peer using given socket connected by remote client.
        /// </summary>
        IPeer Create(Socket socket);
    }
    
    /// <summary>
    /// Interface for implementing peers. Peer represents single remote client and can be used to exchange messages with the client.
    /// </summary>
    public interface IPeer : IDisposable
    {
        #region Events
        /// <summary>
        /// Event invoked when the peer connection has been reset.
        /// </summary>
        event StructEventHandler<PeerResetEventArgs> Reset;
        
        /// <summary>
        /// Event invoked when the peer has incoming messages.
        /// </summary>
        event StructEventHandler<PeerMessageEventArgs> Incoming;
        
        /// <summary>
        /// Event invoked when the peer has outgoing messages.
        /// </summary>
        event StructEventHandler<ServerMessageEventArgs> Outgoing;
        #endregion

        #region Properties
        /// <summary>
        /// Returns the id of the peer.
        /// </summary>
        int Id
        {
            get;
        }
        
        /// <summary>
        /// Returns the remote endpoint of the peer.
        /// </summary>
        IPEndPoint EndPoint
        {
            get;
        }
        #endregion
        
        /// <summary>
        /// Disconnects the peer from the server.
        /// </summary>
        void Disconnect();
        
        /// <summary>
        /// Sends message to the peer.
        /// </summary>
        /// <param name="data">message data to be send</param>
        /// <param name="offset">offset from where the sending begins</param>
        /// <param name="length">length of the message to be send starting from the offset</param>
        void Send(byte[] data, int offset, int length);
        
        /// <summary>
        /// Polls the peer once allowing it to update its internal state and invoke any events in queue. 
        /// </summary>
        void Poll();
    }
    
    /// <summary>
    /// Enumeration defining all possible internal states of a peer.
    /// </summary>
    public enum PeerState : byte
    {
        /// <summary>
        /// Peer is connected and active. Peer can receive and send messages.
        /// </summary>
        Connected = 0,
        
        /// <summary>
        /// Peer is disconnecting. Peer can still receive and send messages but these operations are not guaranteed to complete before the internal connection
        /// is completely closed.
        /// </summary>
        Disconnecting,
        
        /// <summary>
        /// Peer is in disconnected state and should be disposed.
        /// </summary>
        Disconnected
    }
    
    /// <summary>
    /// Enumeration defining all possible reasons why peer was reset.
    /// </summary>
    public enum PeerResetReason : byte
    {
        /// <summary>
        /// Server has reset the peer and disconnected it.
        /// </summary>
        ServerReset = 0,
        
        /// <summary>
        /// Remote client has reset the peer by closing the connection.
        /// </summary>
        RemoteReset,
        
        /// <summary>
        /// Server has reset the peer and disconnected it because the connection has timed out.
        /// </summary>
        TimedOut
    }
    
    /// <summary>
    /// Peer implementation that uses the TCP protocol for communication.
    /// </summary>
    public sealed class TcpPeer : IPeer
    {
        #region Tcp peer factory class
        /// <summary>
        /// Peer factory implementation that creates peers that use the TCP protocol for communication.
        /// </summary>
        public sealed class TcpPeerFactory : IPeerFactory
        {
            #region Fields
            private readonly ArrayPool<byte> pool;
            
            private readonly TimeSpan gracePeriod;
            #endregion
        
            public TcpPeerFactory(ArrayPool<byte> pool, TimeSpan gracePeriod)
            {
                this.pool        = pool ?? throw new ArgumentNullException(nameof(pool));
                this.gracePeriod = gracePeriod;
            }
            
            public IPeer Create(Socket socket)
            {
                if (socket.ProtocolType != ProtocolType.Tcp)
                    throw new ArgumentException("expecting TCP socket");    
            
                return new TcpPeer(socket, pool, gracePeriod);
            }
        }
        #endregion
        
        #region Constant fields
        private const int ReceiveBufferSize = 65536;
        #endregion
        
        #region Static fields
        private static readonly SocketError[] GracefulSocketErrors = 
        {
            SocketError.Shutdown,
            SocketError.ConnectionReset,
            SocketError.NetworkReset,
            SocketError.NotConnected
        };
        
        private static int IdCounter;
        #endregion

        #region Fields
        private readonly ArrayPool<byte> pool;
        private readonly TimeSpan gracePeriod;
        
        private readonly byte[] receiveBuffer;
        private readonly Socket socket;
        
        private readonly LockedDoubleBuffer<PeerMessageEventArgs> incomingMessageBuffer;
        private readonly LockedDoubleBuffer<ServerMessageEventArgs> outgoingMessageBuffer;
        
        private IAsyncResult receiveResult;
        private IAsyncResult disconnectResult;

        private DateTime lastTimeActive;
        
        private PeerResetReason reason;
        private PeerState state;
        #endregion
        
        #region Events
        public event StructEventHandler<PeerResetEventArgs> Reset;
        
        public event StructEventHandler<PeerMessageEventArgs> Incoming;
        public event StructEventHandler<ServerMessageEventArgs> Outgoing;
        #endregion

        #region Properties
        private bool IsConnected => socket.Connected;
        private bool HasTimedOut => (DateTime.UtcNow - lastTimeActive) > gracePeriod;
        
        private bool IsReceivingAsync => !receiveResult?.IsCompleted ?? false;
        private bool IsDisconnectedAsync => disconnectResult?.IsCompleted ?? false;
        
        public int Id
        {
            get;
        }

        public IPEndPoint EndPoint
        {
            get;
        }
        #endregion

        private TcpPeer(Socket socket, ArrayPool<byte> pool, TimeSpan gracePeriod)
        {
            this.pool        = pool;
            this.socket      = socket;
            this.gracePeriod = gracePeriod;

            Id       = IdCounter++;
            EndPoint = (IPEndPoint)socket.RemoteEndPoint;
            
            incomingMessageBuffer = new LockedDoubleBuffer<PeerMessageEventArgs>();
            outgoingMessageBuffer = new LockedDoubleBuffer<ServerMessageEventArgs>();
            receiveBuffer         = new byte[ReceiveBufferSize];
            
            state          = PeerState.Connected;
            lastTimeActive = DateTime.UtcNow;
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
            catch (Exception ex)
            {
                HandleSocketException(ex);
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
            catch (Exception ex)
            {
                HandleSocketException(ex);
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
            
                var message = new byte[length];
            
                Array.Copy(receiveBuffer, 0, message, 0, length);
            
                incomingMessageBuffer.Push(new PeerMessageEventArgs(new PeerConnection(Id, EndPoint), message));   
            }
            catch (Exception ex)
            {
                HandleSocketException(ex);
            }
        }
        #endregion
        
        private void InternalDisconnect(PeerResetReason reason)
        {
            // Omit calls if peer is not in connected state.
            if (state != PeerState.Connected)
                return;
            
            // Only begin disconnecting the socket if it is still connected. 
            if (IsConnected)
                disconnectResult = socket.BeginDisconnect(false, DisconnectCallback, null);
            
            state = PeerState.Disconnecting;
            
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
            lastTimeActive = (outgoingMessages.Length + incomingMessages.Length > 0) ? DateTime.UtcNow : lastTimeActive;
        }
        
        private void UpdateConnectedState()
        {
            if (state != PeerState.Connected) 
                return;
            
            if (!IsConnected)
                InternalDisconnect(PeerResetReason.RemoteReset);
            else if (HasTimedOut)
                InternalDisconnect(PeerResetReason.TimedOut);
            else if (!IsReceivingAsync)
                receiveResult = socket.BeginReceive(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, ReceiveCallback, null);
        }
        
        private void UpdateDisconnectingState()
        {
            if (state != PeerState.Disconnecting)
                return;
            
            if (!IsDisconnectedAsync)
                return;
            
            state = PeerState.Disconnected;
                        
            Reset?.Invoke(this, new PeerResetEventArgs(new PeerConnection(Id, EndPoint), reason));
        }
        
        public void Disconnect()
            => InternalDisconnect(PeerResetReason.ServerReset);

        public void Send(byte[] data, int offset, int length)
        {
            if (state != PeerState.Connected)
                return;
            
            if (!IsConnected)
                return;
            
            socket.BeginSend(data, 
                             offset, 
                             length, 
                             SocketFlags.None, 
                             SendCallback, 
                             new ServerMessageEventArgs(new PeerConnection(Id, EndPoint), data, offset, length));
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
}