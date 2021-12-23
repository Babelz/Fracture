using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Fracture.Net.Messages;
using NLog;

namespace Fracture.Net.Clients
{
    /// <summary>
    /// Client that uses TCP communication protocol. 
    /// </summary>
    public sealed class TcpClient : Client 
    {
        #region Constant fields
        private const int ReceiveBufferSize = 65536;
        #endregion
        
        #region Static fields
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion

        #region Fields
        private readonly byte[] receiveBuffer;
        private readonly Socket socket;
        
        private DateTime lastReceiveTime;

        private IAsyncResult receiveResult;
        #endregion

        #region Properties
        private bool HasTimedOut => (DateTime.UtcNow - lastReceiveTime) >= GracePeriod;
        
        private bool IsReceiving => !receiveResult?.IsCompleted ?? false;
        #endregion
        
        public TcpClient(IMessageSerializer serializer, TimeSpan gracePeriod) 
            : base(serializer, gracePeriod)
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            receiveBuffer   = new byte[ReceiveBufferSize];
            lastReceiveTime = DateTime.UtcNow; 
        }

        #region Async callbacks
        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                var length = socket.EndReceive(ar);
                
                if (length == 0)
                    return;
                
                var offset = 0;
                
                while (offset < length)
                {
                    var size = Serializer.GetSizeFromBuffer(receiveBuffer, offset);
                    
                    if (size <= 0)
                    {
                        Log.Warn("packet contains zero sized message from server");
                        
                        break;
                    }
                    
                    var contents = BufferPool.Take(size);
                    
                    Array.Copy(receiveBuffer, offset, contents, 0, size);
                    
                    var message = Serializer.Deserialize(contents, 0);

                    Updates.Push(new ClientUpdate.Packet(ClientUpdate.PacketOrigin.Remote, message, contents, size));
                    
                    offset += size;
                }
                
                lastReceiveTime = DateTime.UtcNow;
            }
            catch (SocketException e)
            {
                Log.Error(e, "socket error occurred while receiving message");
                
                if (IsConnected)
                    Disconnect();
            }
            catch (ObjectDisposedException)
            {
                Log.Warn("socket already disposed");
            }
        }
        
        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                socket.EndSend(ar);
                
                Updates.Push((ClientUpdate)ar.AsyncState);
            }
            catch (SocketException e)
            {
                Log.Error(e, "socket error occurred while sending message");
                
                Disconnect();
            }
            catch (ObjectDisposedException)
            {
                Log.Warn("socket already disposed");
            }
        }
        
        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                socket.EndConnect(ar);
                            
                UpdateState(ClientState.Connected);
                
                Updates.Push((ClientUpdate)ar.AsyncState);
            }
            catch (SocketException se)
            {
                Log.Error(se, "socket error occurred connecting");
                
                InternalDisconnect(new ClientUpdate.ConnectFailed(se, se.SocketErrorCode));
            }
            catch (ObjectDisposedException ode)
            {
                Log.Warn("socket already disposed");
                
                InternalDisconnect(new ClientUpdate.ConnectFailed(ode));
            }
        }
        
        private void DisconnectCallback(IAsyncResult ar)
        {
            try
            {
                socket.EndDisconnect(ar);
            }
            catch (SocketException e)
            {
                Log.Error(e, "socket error occurred while disconnecting");
            }
            catch (ObjectDisposedException)
            {
                Log.Warn("socket already disposed");
            }
            
            UpdateState(ClientState.Disconnected);
            
            Updates.Push((ClientUpdate)ar.AsyncState);
        }
        #endregion

        private void InternalDisconnect(ClientUpdate update)
        {
            if (State == ClientState.Disconnecting) 
                return;
            
            try
            {
                UpdateState(ClientState.Disconnecting);
                
                socket.BeginDisconnect(true, DisconnectCallback, update);
            }
            catch (SocketException e)
            {
                // In case async call fails due to exception fallback to setting the client status immediately to 
                // disconnected.
                Log.Error(e, "unexpected error occurred while when attempting to start disconnecting, " +
                             "reverting to set state immediately to disconnected");
                
                UpdateState(ClientState.Disconnected);
            
                Updates.Push(update);
            }
        }

        public override void Send(IMessage message)
        {
            base.Send(message);
            
            var size   = Serializer.GetSizeFromMessage(message);
            var buffer = BufferPool.Take(size);
            
            Serializer.Serialize(message, buffer, 0);

            try
            {
                socket.BeginSend(buffer,
                                 0, 
                                 size, 
                                 SocketFlags.None, 
                                 SendCallback, 
                                 new ClientUpdate.Packet(ClientUpdate.PacketOrigin.Local, message, buffer, size)); 
            }
            catch (SocketException e)
            {
                // In case async call fails due to exception begin disconnecting the socket immediately. 
                Log.Error(e, "unexpected error occurred while attempting to start sending data");
                
                Disconnect();
            }
        }

        public override void Connect(IPEndPoint endPoint)
        {
            base.Connect(endPoint);
            
            UpdateState(ClientState.Connecting);
            
            try
            {
                socket.BeginConnect(endPoint, ConnectCallback, new ClientUpdate.Connected(endPoint));
            }
            catch (SocketException e)
            {
                // In case async call fails due to exception begin disconnecting the socket immediately. 
                Log.Error(e, "unexpected error occurred while attempting to start connecting");
                
                Disconnect();
            }
        }

        public override void Disconnect()
        {
            base.Disconnect();
            
            InternalDisconnect(new ClientUpdate.Disconnected(ResetReason.LocalReset));
        }

        public override IEnumerable<ClientUpdate> Poll()
        {
            if (!IsConnected) 
                return Array.Empty<ClientUpdate>();
            
            if (HasTimedOut)
                InternalDisconnect(new ClientUpdate.Disconnected(ResetReason.TimedOut));
            else if (!IsReceiving)
            {
                try
                {
                    receiveResult = socket.BeginReceive(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, ReceiveCallback, null);
                }
                catch (SocketException e)
                {
                    // In case async call fails due to exception begin disconnecting the socket immediately. 
                    Log.Error(e, "unexpected error occurred while attempting to start receiving data with");
                
                    Disconnect();
                }
            }

            return base.Poll();
        }
    }
}