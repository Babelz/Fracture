using System;
using System.Collections.Generic;
using System.ComponentModel;
using Fracture.Net.Hosting;
using Fracture.Net.Hosting.Messaging;
using Fracture.Net.Hosting.Servers;
using Fracture.Net.Messages;
using Fracture.Net.Serialization;
using Fracture.Net.Serialization.Generation;
using Fracture.Net.Tests.Util.Hosting.Fakes;
using Fracture.Net.Tests.Util.Hosting.Utils;
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
            StructSerializer.Map(ObjectSerializationMapper.ForType<TestValueMessage>().PublicProperties().Map());
        }

        public ApplicationPeerTests()
        {
        }

        [Fact]
        public void Should_Handle_Joins_Before_Leaves()
        {
            var handledConnections = new Queue<PeerConnection>();

            var first  = FakePeer.Create();
            var second = FakePeer.Create();

            var application = ApplicationBuilder.FromServer(FakeServer.FromFrames(FakeServerFrame.Create().Join(first),
                                                                                  FakeServerFrame.Create()
                                                                                      .Join(second)
                                                                                      .Leave(first, ResetReason.LocalReset)))
                .Build();

            ApplicationTestUtils.LimitFrames(application, 2);

            application.Join += (object s, in PeerJoinEventArgs e) => { handledConnections.Enqueue(e.Connection); };

            application.Reset += (object s, in PeerResetEventArgs e) => { handledConnections.Enqueue(e.Connection); };

            application.Start();

            Assert.Equal(handledConnections.Dequeue(), first);
            Assert.Equal(handledConnections.Dequeue(), second);
            Assert.Equal(handledConnections.Dequeue(), first);
        }

        [Fact]
        public void Should_Not_Pass_Messages_Forward_If_Peer_Leaves()
        {
            var peerId = FakePeer.Create();

            var application = ApplicationBuilder.FromServer(FakeServer.FromFrames(FakeServerFrame.Create().Join(peerId),
                                                                                  FakeServerFrame.Create()
                                                                                      .Incoming(peerId, Message.Create<TestValueMessage>())
                                                                                      .Leave(peerId, ResetReason.RemoteReset)))
                .Build();

            ApplicationTestUtils.LimitFrames(application, 4);

            application.Requests.Middleware.Use(MiddlewareMatch<RequestMiddlewareContext>.Any(),
                                                (in RequestMiddlewareContext _) =>
                                                    throw new Exception("not expecting messages to reach this part")
            );
        }

        [Fact]
        public void Should_Reset_Peer_If_Response_Resets()
        {
            var first  = FakePeer.Create();
            var second = FakePeer.Create();

            var resetConnections = new List<PeerConnection>();

            var application = ApplicationBuilder.FromServer(FakeServer.FromFrames(FakeServerFrame.Create().Join(first),
                                                                                  FakeServerFrame.Create().Join(second),
                                                                                  FakeServerFrame.Create().Incoming(first, Message.Create<TestValueMessage>())))
                .Build();

            ApplicationTestUtils.LimitFrames(application, 4);

            application.Requests.Router.Use(MessageMatch.Any(), (request, response) => response.Reset());

            application.Reset += (object sender, in PeerResetEventArgs args) => { resetConnections.Add(args.Connection); };

            application.Start();

            Assert.Single(resetConnections);
            Assert.Contains(first, resetConnections);
        }

        [Fact]
        public void Should_Reset_Peer_If_Notification_Resets()
        {
            var first  = FakePeer.Create();
            var second = FakePeer.Create();

            var resetConnections = new List<PeerConnection>();

            var application = ApplicationBuilder.FromServer(FakeServer.FromFrames(FakeServerFrame.Create().Join(first),
                                                                                  FakeServerFrame.Create().Join(second)))
                .Build();

            ApplicationTestUtils.LimitFrames(application, 3);
            ApplicationTestUtils.FrameAction(application, 1, () => application.Notifications.Queue.Enqueue(n => n.Reset(first.PeerId)));

            application.Reset += (object sender, in PeerResetEventArgs args) => { resetConnections.Add(args.Connection); };

            application.Start();

            Assert.Single(resetConnections);
            Assert.Contains(first, resetConnections);
        }
    }
}