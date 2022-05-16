using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Fracture.Net.Messages;
using Serilog;

namespace Fracture.Net.Clients
{
    /// <summary>
    /// Client that uses TCP communication protocol. 
    /// </summary>
    public sealed class TcpClient : Client
    {
        #region Constant fields
        private const int BufferSizes = 65536;
        #endregion

        #region Fields
        private readonly byte [] receiveBuffer;

        private Socket socket;

        private DateTime lastReceiveTime;

        private IAsyncResult receiveResult;
        #endregion

        #region Properties
        private bool HasTimedOut => (DateTime.UtcNow - lastReceiveTime) >= GracePeriod;

        private bool IsReceiving => !receiveResult?.IsCompleted ?? false;

        public override bool IsConnected => socket?.Connected ?? false;
        #endregion

        public TcpClient(IMessageSerializer serializer, TimeSpan gracePeriod)
            : base(serializer, gracePeriod)
        {
            receiveBuffer   = new byte[BufferSizes];
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

                    if (size == 0)
                    {
                        Log.Warning("packet contains zero sized message from server");

                        break;
                    }

                    if (size > length - offset)
                    {
                        Log.Warning($"invalid size in packet, reading further would go outside the bounds of the receive buffer");

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
            catch (SocketException se)
            {
                Log.Error(se, "socket error occurred while receiving message");
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                socket.EndSend(ar);

                Updates.Push((ClientUpdate)ar.AsyncState);
            }
            catch (SocketException se)
            {
                Log.Error(se, "socket error occurred while sending message");
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
                Log.Error(se, "socket error occurred while connecting");

                UpdateState(ClientState.Disconnected);

                Updates.Push(new ClientUpdate.ConnectFailed(se, se.SocketErrorCode));
            }
        }

        private void DisconnectCallback(IAsyncResult ar)
        {
            try
            {
                socket.EndDisconnect(ar);
            }
            catch (SocketException se)
            {
                Log.Error(se, "socket error occurred while disconnecting");
            }

            UpdateState(ClientState.Disconnected);

            Updates.Push((ClientUpdate)ar.AsyncState);

            socket.Dispose();
        }
        #endregion

        private void InternalDisconnect(ClientUpdate update)
        {
            if (State != ClientState.Connected)
                return;

            UpdateState(ClientState.Disconnecting);

            try
            {
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

        private void UpdateConnectedState()
        {
            if (State != ClientState.Connected)
                return;

            if (!IsConnected)
                InternalDisconnect(new ClientUpdate.Disconnected(ResetReason.RemoteReset));
            else if (HasTimedOut)
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

                    InternalDisconnect(new ClientUpdate.Disconnected(ResetReason.RemoteReset));
                }
            }
        }

        public override void Send(in IMessage message)
        {
            if (State != ClientState.Connected)
                return;

            if (!IsConnected)
                return;

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
            catch (SocketException se)
            {
                // In case async call fails due to exception begin disconnecting the socket immediately. 
                Log.Error(se, "unexpected error occurred while attempting to start sending data");

                InternalDisconnect(new ClientUpdate.Disconnected(ResetReason.RemoteReset));
            }
        }

        public override void Connect(IPEndPoint endPoint)
        {
            if (State != ClientState.Disconnected)
                return;

            if (IsConnected)
                return;

            UpdateState(ClientState.Connecting);

            try
            {
                lastReceiveTime = DateTime.UtcNow;

                if (socket != null)
                {
                    socket.Close();
                    socket.Dispose();
                }

                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                {
                    NoDelay           = true,
                    ReceiveBufferSize = BufferSizes,
                    SendBufferSize    = BufferSizes
                };

                socket.BeginConnect(endPoint, ConnectCallback, new ClientUpdate.Connected(endPoint));
            }
            catch (SocketException se)
            {
                // In case async call fails due to exception begin disconnecting the socket immediately. 
                Log.Error(se, "unexpected error occurred while attempting to start connecting");

                InternalDisconnect(new ClientUpdate.ConnectFailed(se, se.SocketErrorCode));
            }
        }

        public override void Disconnect()
        {
            if (State != ClientState.Connected || State != ClientState.Connecting)
                return;

            InternalDisconnect(new ClientUpdate.Disconnected(ResetReason.LocalReset));
        }

        public override IEnumerable<ClientUpdate> Poll()
        {
            UpdateConnectedState();

            return base.Poll();
        }

        public override void Dispose() => socket.Dispose();
    }
}