using System;
using Fracture.Common.Events;

namespace Fracture.Net.Hosting.Servers
{
    public readonly struct PeerJoinEventArgs : IStructEventArgs
    {
        #region Properties
        public PeerConnection Peer
        {
            get;
        }
        
        public TimeSpan Timestamp
        {
            get;
        }
        #endregion

        public PeerJoinEventArgs(PeerConnection peer, TimeSpan timestamp)
        {
            Peer      = peer;
            Timestamp = timestamp;
        }
    }
    
    public readonly struct PeerResetEventArgs : IStructEventArgs
    {
        #region Fields
        public PeerConnection Peer
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
        
        public PeerResetEventArgs(PeerConnection peer, ResetReason reason, TimeSpan timestamp)
        {
            Peer      = peer;
            Reason    = reason;
            Timestamp = timestamp;
        }
    }
    
    public readonly struct PeerMessageEventArgs : IStructEventArgs
    {
        #region Properties
        public PeerConnection Peer
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
        
        public PeerMessageEventArgs(PeerConnection peer, byte[] contents, int length, TimeSpan timestamp)
        {
            Peer      = peer;
            Contents  = contents;
            Length    = length;
            Timestamp = timestamp;
        }
    }
    
    public readonly struct ServerMessageEventArgs : IStructEventArgs
    {
        #region Properties
        public PeerConnection Peer
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
        
        public ServerMessageEventArgs(PeerConnection peer, byte[] contents, int offset, int length, TimeSpan timestamp)
        {
            Peer      = peer;
            Contents  = contents;
            Offset    = offset;
            Length    = length;
            Timestamp = timestamp;
        }
    }
}