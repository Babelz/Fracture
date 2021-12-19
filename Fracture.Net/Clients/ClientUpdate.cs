using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using Fracture.Net.Messages;

namespace Fracture.Net.Clients
{
    /// <summary>
    /// Abstract base class for implementing client events.
    /// </summary>
    public abstract class ClientUpdate
    {
        #region Properties
        public TimeSpan Timestamp
        {
            get;
        }
        #endregion
        
        protected ClientUpdate()
            => Timestamp = DateTime.UtcNow.TimeOfDay;
        
        /// <summary>
        /// Event generated when the client has connected.
        /// </summary>
        public sealed class Connected : ClientUpdate
        {
            #region Properties
            public IPEndPoint RemoteEndPoint
            {
                get;
            }   
            #endregion
            
            public Connected(IPEndPoint remoteEndPoint)
            {
                RemoteEndPoint = remoteEndPoint ?? throw new ArgumentNullException(nameof(remoteEndPoint));
            }
        }

        /// <summary>
        /// Event generated when the client has disconnected.
        /// </summary>
        public sealed class Disconnected : ClientUpdate
        {
            #region Properties
            public ResetReason Reason
            {
                get;
            }

            public Exception Exception
            {
                get;
            }
            #endregion

            public Disconnected(ResetReason reason, Exception exception = null)
            {
                Reason    = reason;
                Exception = exception;
            }
        }

        /// <summary>
        /// Event generated when connecting the client failed.
        /// </summary>
        public sealed class ConnectFailed : ClientUpdate
        {
            #region Properties
            public Exception Exception
            {
                get;
            }

            public SocketError Error
            {
                get;
            }
            #endregion

            public ConnectFailed(Exception exception, SocketError error)
            {
                Exception = exception;
                Error     = error;
            }
        }

        /// <summary>
        /// Enumeration defining packet direction for packet events.
        /// </summary>
        public enum PacketDirection : byte
        {
            /// <summary>
            /// Packet is incoming from remote host.
            /// </summary>
            In = 0,
            
            /// <summary>
            /// Packet is outgoing from the client to the remote host.
            /// </summary>
            Out
        }
        
        /// <summary>
        /// Event generated when the client has received or is sending packet.
        /// </summary>
        public sealed class Packet : ClientUpdate
        {
            #region Properties
            public PacketDirection Direction
            {
                get;
            }
            
            public byte[] Contents
            {
                get;
            }
            public int Length
            {
                get;
            }
            
            public IMessage Message
            {
                get;
            }
            #endregion

            public Packet(PacketDirection direction, IMessage message, byte[] contents, int length)
            {
                Direction = direction;
                Message   = message ?? throw new ArgumentNullException(nameof(message));
                Contents  = contents ?? throw new ArgumentNullException(nameof(contents));
                Length    = length >= 0 ? length : throw new ArgumentOutOfRangeException(nameof(length));
            }
        }
    }
}