using System;
using System.Net;
using System.Net.Sockets;
using Fracture.Common.Events;
using Fracture.Common.Util;

namespace Fracture.Net.Hosting.Peers
{
    public readonly struct PeerJoinEventArgs : IStructEventArgs
    {
        #region Fields
        public PeerConnection Peer
        {
            get;
        }
        
        public TimeSpan Timestamp
        {
            get;
        }
        #endregion

        public PeerJoinEventArgs(in PeerConnection peer, in TimeSpan timestamp)
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
        
        public PeerResetReason Reason
        {
            get;
        }
        
        public TimeSpan Timestamp
        {
            get;
        }
        #endregion
        
        public PeerResetEventArgs(in PeerConnection peer, PeerResetReason reason, in TimeSpan timestamp)
        {
            Peer      = peer;
            Reason    = reason;
            Timestamp = timestamp;
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
        
        public TimeSpan Timestamp
        {
            get;
        }
        #endregion

        public PeerMessageEventArgs(in PeerConnection peer, byte[] data, int length, in TimeSpan timestamp)
        {
            Peer      = peer;
            Data      = data;
            Length    = length;
            Timestamp = timestamp;
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
        
        public TimeSpan Timestamp
        {
            get;
        }
        #endregion

        public ServerMessageEventArgs(in PeerConnection peer, byte[] data, int offset, int length, in TimeSpan timestamp)
        {
            Peer      = peer;
            Offset    = offset;
            Length    = length;
            Data      = data;
            Timestamp = timestamp;
        }
    }
    
    /// <summary>
    /// Structure that represents peer connection.
    /// </summary>
    public readonly struct PeerConnection
    {
        #region Fields
        /// <summary>
        /// Runtime id of the peer.
        /// </summary>
        public readonly int Id;
        
        /// <summary>
        /// Remote end point of the peer.
        /// </summary>
        public readonly IPEndPoint EndPoint;
        #endregion
        
        public PeerConnection(int id, IPEndPoint endPoint)
        {
            Id       = id;
            EndPoint = endPoint;
        }

        public override string ToString()
            => $"{Id}, {EndPoint.Address}:{EndPoint.Port}";

        public override int GetHashCode()
            => HashUtils.Create()
                        .Append(Id)
                        .Append(EndPoint)
                        .Append(EndPoint.Port);
    }
    
    /// <summary>
    /// Enumeration defining all possible internal states of a peer.
    /// </summary>
    public enum PeerState : byte
    {
        /// <summary>
        /// Peer is connected and active. Peer can receive and send messages.
        /// </summary>
        Connected = 0,
        
        /// <summary>
        /// Peer is disconnecting. Peer can still receive and send messages but these operations are not guaranteed to complete before the internal connection
        /// is completely closed.
        /// </summary>
        Disconnecting,
        
        /// <summary>
        /// Peer is in disconnected state and should be disposed.
        /// </summary>
        Disconnected
    }
    
    /// <summary>
    /// Enumeration defining all possible reasons why peer was reset.
    /// </summary>
    public enum PeerResetReason : byte
    {
        /// <summary>
        /// Server has reset the peer and disconnected it.
        /// </summary>
        ServerReset = 0,
        
        /// <summary>
        /// Remote client has reset the peer by closing the connection.
        /// </summary>
        RemoteReset,
        
        /// <summary>
        /// Server has reset the peer and disconnected it because the connection has timed out.
        /// </summary>
        TimedOut
    }
    
    /// <summary>
    /// Interface for implementing peers. Peer represents single remote client and can be used to exchange messages with the client.
    /// </summary>
    public interface IPeer : IDisposable
    {
        #region Events
        /// <summary>
        /// Event invoked when the peer connection has been reset.
        /// </summary>
        event StructEventHandler<PeerResetEventArgs> Reset;
        
        /// <summary>
        /// Event invoked when the peer has incoming messages.
        /// </summary>
        event StructEventHandler<PeerMessageEventArgs> Incoming;
        
        /// <summary>
        /// Event invoked when the peer has outgoing messages.
        /// </summary>
        event StructEventHandler<ServerMessageEventArgs> Outgoing;
        #endregion

        #region Properties
        /// <summary>
        /// Returns the id of the peer.
        /// </summary>
        int Id
        {
            get;
        }
        
        /// <summary>
        /// Returns the remote endpoint of the peer.
        /// </summary>
        IPEndPoint EndPoint
        {
            get;
        }
        
        /// <summary>
        /// Returns boolean declaring whether the peer is still connected.
        /// </summary>
        bool Connected
        {
            get;
        }
        #endregion
        
        /// <summary>
        /// Disconnects the peer from the server.
        /// </summary>
        void Disconnect();
        
        /// <summary>
        /// Sends message to the peer.
        /// </summary>
        /// <param name="data">message data to be send</param>
        /// <param name="offset">offset from where the sending begins</param>
        /// <param name="length">length of the message to be send starting from the offset</param>
        void Send(byte[] data, int offset, int length);
        
        /// <summary>
        /// Polls the peer once allowing it to update its internal state and invoke any events in queue. 
        /// </summary>
        void Poll();
    }
    
    /// <summary>
    /// Interface for implementing peer factories.
    /// </summary>
    public interface IPeerFactory
    {
        /// <summary>
        /// Creates new peer using given socket connected by remote client.
        /// </summary>
        IPeer Create(Socket socket);
    }
}