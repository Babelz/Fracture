using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Fracture.Common;
using Fracture.Common.Collections.Concurrent;
using Fracture.Net.Messages;

namespace Fracture.Net.Clients
{
    /// <summary>
    /// Enumeration defining all possible client states.
    /// </summary>
    public enum ClientState : byte
    {
        /// <summary>
        /// Client is disconnected and is ready to start connecting.
        /// </summary>
        Disconnected = 0,
        
        /// <summary>
        /// Client is currently establishing connection to the remote host.
        /// </summary>
        Connecting,
        
        /// <summary>
        /// Client is connected to the remote host and is ready for messaging operations.
        /// </summary>
        Connected,
        
        /// <summary>
        /// Client is disconnecting from remote host.
        /// </summary>
        Disconnecting
    }

    /// <summary>
    /// Abstract base class for implementing clients. Clients provide state management for connections and can support variety of different communication
    /// protocols. Clients are reusable as long as they are not disposed.
    /// </summary>
    public abstract class Client
    {
        #region Fields
        private long state;
        #endregion
        
        #region Properties
        /// <summary>
        /// Gets the current state of the client.
        /// </summary>
        protected ClientState State
        {
            get => (ClientState)Interlocked.Read(ref state);
            private set => Interlocked.Exchange(ref state, (long)value);
        }
        
        /// <summary>
        /// Gets the buffer that is used for buffering the client events.
        /// </summary>
        protected LockedDoubleBuffer<ClientUpdate> Updates
        {
            get;
        }
        
        /// <summary>
        /// Gets the message serializer used for message serialization.
        /// </summary>
        protected IMessageSerializer Serializer
        {
            get;
        }
        
        /// <summary>
        /// Gets the socket that this client manages.
        /// </summary>
        protected Socket Socket
        {
            get;
        }
        
        /// <summary>
        /// Gets the max grace period for which the client is allowed to be idle.
        /// </summary>
        public TimeSpan GracePeriod
        {
            get;
        }

        /// <summary>
        /// Gets boolean declaring whether the client is still connected.
        /// </summary>
        public bool Connected => State == ClientState.Connected;
        #endregion
        
        protected Client(Socket socket, IMessageSerializer serializer, TimeSpan gracePeriod)
        {
            GracePeriod = gracePeriod;
            Serializer  = serializer ?? throw new ArgumentNullException(nameof(serializer));
            Socket      = socket ?? throw new ArgumentNullException(nameof(socket));
            
            Updates = new LockedDoubleBuffer<ClientUpdate>();
        }
        
        private static void ThrowInvalidStateTransition(ClientState current, ClientState next)
            => throw new InvalidOperationException($"illegal state transition from {current} to {next}");
        
        protected void UpdateState(ClientState next)
        {
            var current = State;
            
            switch (next)
            {
                case ClientState.Disconnected:
                    if (current != ClientState.Disconnecting && current != ClientState.Connecting)
                        ThrowInvalidStateTransition(current, next);
                    break;
                case ClientState.Connecting:
                    if (current != ClientState.Disconnected)
                        ThrowInvalidStateTransition(current, next);
                    break;
                case ClientState.Connected:
                    if (current != ClientState.Connecting)
                        ThrowInvalidStateTransition(current, next);
                    break;
                case ClientState.Disconnecting:
                    if (current != ClientState.Connected)
                        ThrowInvalidStateTransition(current, next);
                    break;
                default:
                    throw new InvalidOrUnsupportedException(nameof(next), next);
            }
            
            State = next;
        }
        
        public virtual void Send(IMessage message)
        {
            if (State != ClientState.Connected)
                throw new InvalidOperationException($"calls to {nameof(Send)} are invalid when client state is not {nameof(ClientState.Connected)}");
            
            if (message == null)
                throw new ArgumentNullException(nameof(message));
        }
        
        public virtual void Disconnect()
        {
            if (State != ClientState.Connected)
                throw new InvalidOperationException($"calls to {nameof(Disconnect)} are invalid when client state is not {nameof(ClientState.Connected)}");
        }

        public virtual void Connect(IPEndPoint endPoint)
        {
            if (State != ClientState.Disconnected)
                throw new InvalidOperationException($"calls to {nameof(Connect)} are invalid when client state is not {nameof(ClientState.Disconnected)}");
            
            if (endPoint == null)
                throw new ArgumentNullException(nameof(endPoint));
        }

        public virtual IEnumerable<ClientUpdate> Poll()
            => Updates.Read();

        public virtual void Dispose()
            => Socket?.Dispose();
    }
}