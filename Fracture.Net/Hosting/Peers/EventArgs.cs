using Fracture.Common.Events;

namespace Fracture.Net.Hosting.Peers
{
    public readonly struct PeerJoinEventArgs : IStructEventArgs
    {
        #region Fields
        public PeerConnection Peer
        {
            get;
        }
        #endregion

        public PeerJoinEventArgs(in PeerConnection peer)
            => Peer = peer;
    }
    
    public readonly struct PeerResetEventArgs : IStructEventArgs
    {
        #region Fields
        public PeerConnection Peer
        {
            get;
        }
        
        public PeerResetReason Reason
        {
            get;
        }
        #endregion
        
        public PeerResetEventArgs(in PeerConnection peer, PeerResetReason reason)
        {
            Peer   = peer;
            Reason = reason;
        }
    }
    
    public readonly struct ServerMessageEventArgs : IStructEventArgs
    {
        #region Fields
        public PeerConnection Peer
        {
            get;
        }
        
        public byte[] Data
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
        #endregion
        
        public ServerMessageEventArgs(in PeerConnection peer, byte[] data, int offset, int length)
        {
            Peer   = peer;
            Offset = offset;
            Length = length;
            Data   = data;
        }
    }

    public readonly struct PeerMessageEventArgs : IStructEventArgs
    {
        #region Fields
        public PeerConnection Peer
        {
            get;
        }
        
        public byte[] Data
        {
            get;
        }
        public int Length
        {
            get;
        }
        #endregion

        public PeerMessageEventArgs(in PeerConnection peer, byte[] data, int length)
        {
            Peer   = peer;
            Data   = data;
            Length = length;
        }
    }
}