using System;
using System.Collections.Generic;
using System.ComponentModel;
using Fracture.Common.Di.Binding;
using Fracture.Net.Hosting;
using Fracture.Net.Hosting.Messaging;
using Fracture.Net.Hosting.Servers;
using Fracture.Net.Tests.Hosting.Fakes;
using Fracture.Net.Tests.Hosting.Utils;
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
            
            var first  = FakePeer.Create();
            var second = FakePeer.Create();
            
            var frames = new []
            {
                FakeServerFrame.Create().Join(first),
                FakeServerFrame.Create().Join(second).Leave(first, PeerResetReason.ServerReset),
            };
            
            var application = ApplicationBuilder.Create()
                                                .Server(FakeServer.Create(frames))
                                                .Script<TickLimiterScript>(BindingValue.Const("limit", 2))
                                                .Build();
            
            application.Join += (s, e) =>
            {
                handledPeers.Enqueue(e.Peer);
            };
            
            application.Reset += (s, e) =>
            {
                handledPeers.Enqueue(e.Peer);
            };
            
            application.Start(FakeServer.Port, FakeServer.Backlog);
            
            Assert.Equal(handledPeers.Dequeue(), first);
            Assert.Equal(handledPeers.Dequeue(), second);
            Assert.Equal(handledPeers.Dequeue(), first);
        }
        
        [Fact]
        public void Should_Not_Pass_Messages_Forward_If_Peer_Leaves()
        {
            var peer = FakePeer.Create();
            
            var frames = new []
            {
                FakeServerFrame.Create().Join(peer),
                FakeServerFrame.Create().Incoming(peer, new byte[16], 16).Leave(peer, PeerResetReason.RemoteReset)
            };
            
            var application = ApplicationBuilder.Create()
                                                .Server(FakeServer.Create(frames))
                                                .Script<TickLimiterScript>(BindingValue.Const("limit", 2))
                                                .Build();
            
            var requests = 0;
            
            application.Request.Middleware.Use(MiddlewareMatch<RequestMiddlewareContext>.Any(), (in RequestMiddlewareContext _) =>
            {
                requests++;
                
                return MiddlewareHandlerResult.Accept;
            }); 

            application.Start(FakeServer.Port, FakeServer.Backlog);
            
            Assert.Equal(0, requests);
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