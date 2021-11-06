using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Fracture.Common.Collections.Concurrent;
using Fracture.Common.Events;

namespace Fracture.Net.Hosting
{
    /// <summary>
    /// Structure event arguments used when new connection is made to a listener.
    /// </summary>
    public readonly struct ListenerConnectedEventArgs : IStructEventArgs
    {
        #region Fields
        /// <summary>
        /// Socket of the new connection.
        /// </summary>
        public readonly Socket Socket;
        #endregion
        
        public ListenerConnectedEventArgs(Socket socket)
            => Socket = socket;
    }
    
    /// <summary>
    /// Interface for implementing listeners that provide endpoint for clients to connect to.
    /// </summary>
    public interface IListener : IDisposable
    {
        #region Events
        /// <summary>
        /// Event invoked when new connection is made to the listener.
        /// </summary>
        event StructEventHandler<ListenerConnectedEventArgs> Connected;
        #endregion
        
        /// <summary>
        /// Places the listener to listen state and begins listening for new connections at given port with given backlog size.
        /// </summary>
        /// <param name="port">port the listener will be bound to</param>
        /// <param name="backlog">how many pending clients should be accepted</param>
        void Listen(int port, int backlog);
        
        /// <summary>
        /// Stops listening for any incoming connections and stops accepting them.
        /// </summary>
        void Stop();
        
        /// <summary>
        /// Polls the listener and accepts all incoming connections if the listener is in listening state.
        /// </summary>
        void Poll();
    }
    
    /// <summary>
    /// Listener implementation that listens for incoming TPC connections.
    /// </summary>
    public sealed class TcpListener : IListener
    {
        #region Fields
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
        
        public TcpListener()
        {
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
            catch (SocketException ex)
            {
                // Only expect the socket to be shutdown if an exception occurs.
                if (ex.SocketErrorCode != SocketError.Shutdown)
                    throw;
            }
        }
        #endregion
        
        public void Listen(int port, int backlog)
        {   
            if (Accepting)
                throw new InvalidOperationException("listener already listening");
            
            Accepting = true;
            
            // Place socket in listening state and listen on all interfaces on given port.
            listener.Bind(new IPEndPoint(IPAddress.Any, port));
            
            listener.Listen(backlog);
        }

        public void Stop()
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
                Connected?.Invoke(this, new ListenerConnectedEventArgs(newConnection));
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