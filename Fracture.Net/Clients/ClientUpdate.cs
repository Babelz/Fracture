using System;
using System.Net;
using System.Net.Sockets;
using Fracture.Net.Messages;
using Newtonsoft.Json;

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

        public override string ToString()
            => JsonConvert.SerializeObject(this);

        /// <summary>
        /// Event generated when the client has connected.
        /// </summary>
        public sealed class Connected : ClientUpdate
        {
            #region Properties
            [JsonIgnore]
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

            public ConnectFailed(Exception exception, SocketError error = SocketError.Success)
            {
                Exception = exception;
                Error     = error;
            }
        }

        /// <summary>
        /// Enumeration defining packet direction based on origin.
        /// </summary>
        public enum PacketOrigin : byte
        {
            /// <summary>
            /// Packet is incoming from remote host.
            /// </summary>
            Remote = 0,

            /// <summary>
            /// Packet is outgoing from the client to the remote host.
            /// </summary>
            Local
        }

        /// <summary>
        /// Event generated when the client has received or is sending packet.
        /// </summary>
        public sealed class Packet : ClientUpdate
        {
            #region Properties
            /// <summary>
            /// Gets the origin of the packet.
            /// </summary>
            public PacketOrigin Origin
            {
                get;
            }

            /// <summary>
            /// Gets the contents of the packet.
            /// </summary>
            public byte[] Contents
            {
                get;
            }

            /// <summary>
            /// Gets the length of the packet.
            /// </summary>
            public int Length
            {
                get;
            }

            /// <summary>
            /// Gets the message in the packet.
            /// </summary>
            public IMessage Message
            {
                get;
            }
            #endregion

            public Packet(PacketOrigin origin, in IMessage message, byte[] contents, int length)
            {
                Origin   = origin;
                Message  = message ?? throw new ArgumentNullException(nameof(message));
                Contents = contents ?? throw new ArgumentNullException(nameof(contents));
                Length   = length >= 0 ? length : throw new ArgumentOutOfRangeException(nameof(length));
            }
        }
    }
}