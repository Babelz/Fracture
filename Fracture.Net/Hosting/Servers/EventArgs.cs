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
        
        public byte[] Contents
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