using System.Collections.Generic;
using Fracture.Common.Events;
using Fracture.Net.Hosting.Peers;
using Fracture.Net.Hosting.Servers;

namespace Fracture.Net.Tests.Hosting.Fakes
{
    public sealed class FakeServerFrame
    {
        public FakeServerFrame()
        {
        }
    }
    
    public sealed class FakeServerFrameBuilder
    {
        protected FakeServerFrameBuilder()
        {
        }

        public static FakeServerFrameBuilder Create()
            => new FakeServerFrameBuilder();
    }
    
    public sealed class FakeServer : IServer
    {
        public event StructEventHandler<PeerJoinEventArgs> Join;
        public event StructEventHandler<PeerResetEventArgs> Reset;
        public event StructEventHandler<PeerMessageEventArgs> Incoming;
        public event StructEventHandler<ServerMessageEventArgs> Outgoing;

        public int PeersCount
        {
            get;
        }

        public IEnumerable<int> Peers
        {
            get;
        }

        public void Disconnect(int id)
        {
            throw new System.NotImplementedException();
        }

        public bool Connected(int id)
        {
            throw new System.NotImplementedException();
        }

        public void Send(int id, byte[] data, int offset, int length)
        {
            throw new System.NotImplementedException();
        }

        public void Start(int port, int backlog)
        {
            throw new System.NotImplementedException();
        }

        public void Shutdown()
        {
            throw new System.NotImplementedException();
        }

        public void Poll()
        {
            throw new System.NotImplementedException();
        }
        
        public void Dispose()
        {
            throw new System.NotImplementedException();
        }
    }
}