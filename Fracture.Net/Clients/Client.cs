using System;
using System.Net;
using System.Net.Sockets;
using Fracture.Net.Messages;

namespace Fracture.Net.Clients
{
    public interface IClient : IDisposable
    {
        void Send(IMessage message);
        
        void Disconnect();
        
        void Connect(string ip, int port);
        
        ClientEvent Poll();
    }
    
    public enum ClientState : byte
    {
        Connecting = 0,
        Connected,
        Disconnected,
        
    }

    public abstract class Client : IClient
    {
        #region Properties
        protected IMessageSerializer Serializer
        {
            get;
        }
        
        protected Socket Socket
        {
            get;
        }
        #endregion
        
        #region Events
        public abstract event EventHandler<ConnectedEventArgs> Connected;
        
        public abstract event EventHandler<DisconnectedEventArgs> Disconnected;
        public abstract event EventHandler<ConnectFailedEventArgs> ConnectFailed;
        
        public abstract event EventHandler<IncomingMessageEventArgs> Incoming;
        public abstract event EventHandler<OutgoingMessageEventArgs> Outgoing;
        #endregion

        protected Client(IMessageSerializer serializer, Socket socket)
        {
            Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            Socket     = socket ?? throw new ArgumentNullException(nameof(socket));
        }
        
        public abstract void Send(IMessage message);
        
        public abstract void Disconnect();

        public abstract void Connect(string ip, int port);

        public abstract void Poll();

        public virtual void Dispose()
            => Socket?.Dispose();
    }
}