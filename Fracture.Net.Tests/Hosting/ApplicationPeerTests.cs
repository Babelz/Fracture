using System;
using System.Collections.Generic;
using System.ComponentModel;
using Fracture.Common.Di.Binding;
using Fracture.Net.Hosting;
using Fracture.Net.Hosting.Messaging;
using Fracture.Net.Hosting.Servers;
using Fracture.Net.Messages;
using Fracture.Net.Serialization;
using Fracture.Net.Serialization.Generation;
using Fracture.Net.Tests.Hosting.Fakes;
using Fracture.Net.Tests.Hosting.Utils;
using Xunit;

namespace Fracture.Net.Tests.Hosting
{
    [Category("Hosting")]
    public class ApplicationPeerTests
    {
        #region Test message types
        private sealed class TestValueMessage : IMessage
        {
            #region Properties
            public int Value
            {
                get;
                private set;
            }
            #endregion
            
            public TestValueMessage()
            {
            }

            public void Clear()
                => Value = default;
        }
        #endregion

        static ApplicationPeerTests()
        {
            StructSerializer.Map(ObjectSerializationMapper.Create().FromType<TestValueMessage>().PublicProperties().Map());
        }
        
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
                                                .Script<TickLimiterScript>(BindingValue.Const("limit", (ulong)2))
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
                FakeServerFrame.Create().Incoming(peer, Message.Take<TestValueMessage>()).Leave(peer, PeerResetReason.RemoteReset)
            };
            
            var application = ApplicationBuilder.Create()
                                                .Server(FakeServer.Create(frames))
                                                .Script<TickLimiterScript>(BindingValue.Const("limit", (ulong)2))
                                                .Build();
            
            application.Request.Middleware.Use(MiddlewareMatch<RequestMiddlewareContext>.Any(), (in RequestMiddlewareContext _) =>
                throw new Exception("not expecting messages to reach this part")
            );

            application.Start(FakeServer.Port, FakeServer.Backlog);
        }
        
        [Fact]
        public void Should_Reset_Peer_If_Response_Resets()
        {
            var first  = FakePeer.Create();
            var second = FakePeer.Create();

            var frames = new []
            {
                FakeServerFrame.Create().Join(first),
                FakeServerFrame.Create().Join(second),
                FakeServerFrame.Create().Incoming(first, Message.Take<TestValueMessage>())
            };

            var resetPeers = new List<PeerConnection>();
            
            var application = ApplicationBuilder.Create()
                                                .Server(FakeServer.Create(frames))
                                                .Script<TickLimiterScript>(BindingValue.Const("limit", (ulong)4))
                                                .Build();
            
            application.Request.Router.Use(MessageMatch.Any(), (request, response) => response.Reset());

            application.Reset += (sender, args) => resetPeers.Add(args.Peer);
            
            application.Start(FakeServer.Port, FakeServer.Backlog);
            
            Assert.Single(resetPeers);
            Assert.Contains(first, resetPeers);
        }
        
        [Fact]
        public void Should_Reset_Peer_If_Notification_Resets()
        {
            var first  = FakePeer.Create();
            var second = FakePeer.Create();

            var frames = new []
            {
                FakeServerFrame.Create().Join(first),
                FakeServerFrame.Create().Join(second)
            };

            var resetPeers = new List<PeerConnection>();

            var application = ApplicationBuilder.Create()
                                                .Server(FakeServer.Create(frames))
                                                .Script<TickLimiterScript>(BindingValue.Const("limit", (ulong)3))
                                                .Script<FrameActorScript>(BindingValue.Const("actions", new []
                                                 {
                                                     FrameAction.Create(1, (a) => a.Notification.Queue.Enqueue().Reset(first.Id)) 
                                                 }))
                                                .Build();

            application.Reset += (sender, args) => resetPeers.Add(args.Peer);
            
            application.Start(FakeServer.Port, FakeServer.Backlog);
            
            Assert.Single(resetPeers);
            Assert.Contains(first, resetPeers);
        }
    }
}