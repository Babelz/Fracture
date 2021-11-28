using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Threading.Tasks;
using Fracture.Net.Hosting;
using Fracture.Net.Hosting.Peers;
using Fracture.Net.Tests.Hosting.Fakes;
using Xunit;

namespace Fracture.Net.Tests.Hosting
{
    [Category("Hosting")]
    public class ApplicationPeerTests
    {
        public ApplicationPeerTests()
        {
        }
        
        [Fact]
        public void Should_Handle_Joins_Before_Leaves()
        {
            var handledPeers = new Queue<PeerConnection>();
            
            var joiningPeer = FakePeer.Create();
            var leavingPeer = FakePeer.Create();
            var frames      = FakeServerFrame.Create()
                                             .Join(joiningPeer)
                                             .Leave(leavingPeer, PeerResetReason.RemoteReset);
            
            var application = ApplicationBuilder.Create()
                                                .Server(new FakeServer(frames))
                                                .Build();
            
            application.Join += (s, e) =>
            {
                handledPeers.Enqueue(e.Peer);
            };
            
            application.Reset += (s, e) =>
            {
                handledPeers.Enqueue(e.Peer);
            };
        }
        
        [Fact]
        public void Should_Not_Pass_Messages_Forward_If_Peer_Leaves()
        {
        }
        
        [Fact]
        public void Should_Reset_Peer_Next_Frame_If_Response_Resets()
        {
        }
        
        [Fact]
        public void Should_Reset_Peer_Next_Frame_If_Notification_Resets()
        {
        }
    }
}