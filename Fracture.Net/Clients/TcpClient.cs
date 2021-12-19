using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Fracture.Net.Messages;
using Newtonsoft.Json;
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
        
        private DateTime lastReceiveTime;
        
        private IAsyncResult receiveResult;
        #endregion

        #region Properties
        private bool HasTimedOut => (DateTime.UtcNow - lastReceiveTime) >= GracePeriod;
        
        private bool IsReceiving => !receiveResult?.IsCompleted ?? false;
        #endregion
        
        public TcpClient(IMessageSerializer serializer, TimeSpan gracePeriod) 
            : base(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp), serializer, gracePeriod)
        {
            receiveBuffer   = new byte[ReceiveBufferSize];
            lastReceiveTime = DateTime.UtcNow; 
        }

        #region Async callbacks
        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                var length = Socket.EndReceive(ar);
                
                if (length == 0)
                    return;
            
                var contents = BufferPool.Take(length);
            
                Array.Copy(receiveBuffer, 0, contents, 0, length);
                
                var message = Serializer.Deserialize(contents, 0);
            
                Updates.Push(new ClientUpdate.Packet(ClientUpdate.PacketOrigin.Remote, message, contents, length));  
                
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
                Socket.EndSend(ar);
                
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
                Socket.EndConnect(ar);
                            
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
                Socket.EndDisconnect(ar);
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
            
            UpdateState(ClientState.Disconnecting);
            
            Socket.BeginDisconnect(true, DisconnectCallback, update);
        }

        public override void Send(IMessage message)
        {
            base.Send(message);
            
            var size   = Serializer.GetSizeFromMessage(message);
            var buffer = BufferPool.Take(size);
            
            Serializer.Serialize(message, buffer, 0);
            
            Socket.BeginSend(buffer, 
                             0, 
                             size, 
                             SocketFlags.None, 
                             SendCallback, new ClientUpdate.Packet(ClientUpdate.PacketOrigin.Local, message, buffer, size)); 
        }

        public override void Connect(IPEndPoint endPoint)
        {
            base.Connect(endPoint);
            
            UpdateState(ClientState.Connecting);
            
            Socket.BeginConnect(endPoint, ConnectCallback, new ClientUpdate.Connected(endPoint));
        }

        public override void Disconnect()
        {
            base.Disconnect();
            
            InternalDisconnect(new ClientUpdate.Disconnected(ResetReason.LocalReset));
        }

        public override IEnumerable<ClientUpdate> Poll()
        {
            if (IsConnected)
            {
                if (HasTimedOut)
                    InternalDisconnect(new ClientUpdate.Disconnected(ResetReason.TimedOut));
                else if (!IsReceiving)
                    receiveResult = Socket.BeginReceive(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, ReceiveCallback, null);
            }
            
            return base.Poll();
        }
    }
}