using System;
using Fracture.Common.Events;

namespace Fracture.Net.Hosting.Servers
{
    public readonly struct PeerJoinEventArgs : IStructEventArgs
    {
        #region Properties
        public PeerConnection Connection
        {
            get;
        }

        public TimeSpan Timestamp
        {
            get;
        }
        #endregion

        public PeerJoinEventArgs(in PeerConnection connection, in TimeSpan timestamp)
        {
            Connection = connection;
            Timestamp  = timestamp;
        }
    }

    public readonly struct PeerResetEventArgs : IStructEventArgs
    {
        #region Fields
        public PeerConnection Connection
        {
            get;
        }

        public ResetReason Reason
        {
            get;
        }

        public TimeSpan Timestamp
        {
            get;
        }
        #endregion

        public PeerResetEventArgs(in PeerConnection connection, ResetReason reason, in TimeSpan timestamp)
        {
            Connection = connection;
            Reason     = reason;
            Timestamp  = timestamp;
        }
    }

    public readonly struct PeerMessageEventArgs : IStructEventArgs
    {
        #region Properties
        public PeerConnection Connection
        {
            get;
        }

        /// <summary>
        /// Gets the raw serialized contents of this message.
        /// </summary>
        public byte[] Contents
        {
            get;
        }

        /// <summary>
        /// Gets the length of bytes received from the client. This is the safe amount of bytes you can read from the <see cref="Contents"/>.
        /// </summary>
        public int Length
        {
            get;
        }

        /// <summary>
        /// Gets the timestamp when this message was received by the application.
        /// </summary>
        public TimeSpan Timestamp
        {
            get;
        }
        #endregion

        public PeerMessageEventArgs(in PeerConnection connection, byte[] contents, int length, in TimeSpan timestamp)
        {
            Connection = connection;
            Contents   = contents;
            Length     = length;
            Timestamp  = timestamp;
        }
    }

    public readonly struct ServerMessageEventArgs : IStructEventArgs
    {
        #region Properties
        public PeerConnection Connection
        {
            get;
        }

        public byte[] Contents
        {
            get;
        }

        public int Offset
        {
            get;
        }

        public int Length
        {
            get;
        }

        public TimeSpan Timestamp
        {
            get;
        }
        #endregion

        public ServerMessageEventArgs(in PeerConnection connection, byte[] contents, int offset, int length, in TimeSpan timestamp)
        {
            Connection = connection;
            Contents   = contents;
            Offset     = offset;
            Length     = length;
            Timestamp  = timestamp;
        }
    }
}