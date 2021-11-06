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
}