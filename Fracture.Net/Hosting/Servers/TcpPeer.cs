using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using Fracture.Common.Collections.Concurrent;

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
            SocketError.NotConnected
        };
        
        private static int IdCounter;
        #endregion

        #region Fields
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
        public event EventHandler<PeerResetEventArgs> Reset;
        
        public event EventHandler<PeerMessageEventArgs> Incoming;
        public event EventHandler<ServerMessageEventArgs> Outgoing;
        #endregion

        #region Properties
        private bool IsConnected => socket.Connected;
        private bool HasTimedOut => (DateTime.UtcNow - lastTimeActive) > gracePeriod;
        
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
        
        public bool Connected 
            => state == PeerState.Connected;
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
            
                var contents = ServerResources.BlockBuffer.Take(length);
            
                Array.Copy(receiveBuffer, 0, contents, 0, length);
            
                incomingMessageBuffer.Push(ServerResources.EventArgs.PeerMessage.Take(args =>
                {
                    args.Peer      = new PeerConnection(Id, EndPoint);
                    args.Contents  = contents;
                    args.Length    = length;
                    args.Timestamp = DateTime.UtcNow.TimeOfDay;
                }));   
            }
            catch (Exception e)
            {
                HandleSocketException(e);
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
            else if (!IsReceiving)
                receiveResult = socket.BeginReceive(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, ReceiveCallback, null);
        }
        
        private void UpdateDisconnectingState()
        {
            if (state != PeerState.Disconnecting)
                return;
            
            if (!HasDisconnected)
                return;
            
            state = PeerState.Disconnected;
                        
            Reset?.Invoke(this, ServerResources.EventArgs.PeerReset.Take(args =>
            {
                args.Peer      = new PeerConnection(Id, EndPoint);
                args.Reason    = reason;
                args.Timestamp = DateTime.UtcNow.TimeOfDay;
            }));
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
                             ServerResources.EventArgs.ServerMessage.Take(args =>
                             {
                                args.Peer      = new PeerConnection(Id, EndPoint);
                                args.Contents  = data;
                                args.Offset    = offset;
                                args.Length    = length;
                                args.Timestamp = DateTime.UtcNow.TimeOfDay;
                             }));
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
        #region Fields
        private readonly TimeSpan gracePeriod;
        #endregion
        
        public TcpPeerFactory(TimeSpan gracePeriod)
            => this.gracePeriod = gracePeriod;
        
        public IPeer Create(Socket socket)
        {
            if (socket.ProtocolType != ProtocolType.Tcp)
                throw new ArgumentException("expecting TCP socket");    
            
            return new TcpPeer(socket, gracePeriod);
        }
    }
}