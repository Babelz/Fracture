using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Fracture.Common.Collections.Concurrent;
using Fracture.Common.Events;

namespace Fracture.Net.Hosting.Servers
{
    /// <summary>
    /// Listener implementation that listens for incoming TPC connections.
    /// </summary>
    public sealed class TcpListener : IListener
    {
        #region Fields
        private readonly int port;
        private readonly int backlog;
        
        private readonly LockedDoubleBuffer<Socket> newConnections;
        private readonly Socket listener;
        
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
        private bool Listening => (!listenResult?.IsCompleted ?? false);
        #endregion
        
        public TcpListener(int port, int backlog = 0)
        {
            this.port    = port;
            this.backlog = backlog;
            
            listener       = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
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
}