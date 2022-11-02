using System;
using System.Collections.Generic;
using System.Linq;
using Fracture.Common;
using Fracture.Common.Di.Attributes;
using Fracture.Net.Hosting.Messaging;
using Fracture.Net.Hosting.Servers;
using Fracture.Net.Messages;
using Serilog;

namespace Fracture.Net.Hosting.Services
{
    /// <summary>
    /// Interface for implementing services that provide latency recording of peers.
    /// </summary>
    public interface ILatencyService : IApplicationService
    {
        /// <summary>
        /// Records ping timestamp for peer with given id.
        /// </summary>
        ulong RecordPing(int peerId);

        /// <summary>
        /// Records pong timestamp for peer with given id. Returns boolean declaring whether request pair was found.
        /// </summary>
        bool RecordPong(int peerId, ulong requestId);

        TimeSpan GetAverage(int peerId);
        TimeSpan GetMax(int peerId);
        TimeSpan GetMin(int peerId);

        /// <summary>
        /// Resets latency metrics for peer with given id.
        /// </summary>
        void Reset(int peerId);
    }

    public sealed class LatencyService : ApplicationService, ILatencyService
    {
        #region Static fields
        private static ulong RequestCounter = 0u;
        #endregion

        #region Fields
        // Lookup containing each peers received samples.
        private readonly Dictionary<int, Queue<TimeSpan>> samples;

        // Lookup containing each peers ping requests.
        private readonly Dictionary<int, Dictionary<ulong, TimeSpan>> pings;

        private readonly int maxSamples;
        #endregion

        [BindingConstructor]
        public LatencyService(IApplicationServiceHost application, int maxSamples = 10)
            : base(application)
        {
            this.maxSamples = maxSamples > 0 ? maxSamples : throw new ArgumentOutOfRangeException(nameof(maxSamples));

            samples = new Dictionary<int, Queue<TimeSpan>>();
            pings   = new Dictionary<int, Dictionary<ulong, TimeSpan>>();
        }

        public ulong RecordPing(int peerId)
        {
            // Create latency "session" if it does not exist.
            if (!samples.ContainsKey(peerId))
            {
                samples.Add(peerId, new Queue<TimeSpan>());

                pings.Add(peerId, new Dictionary<ulong, TimeSpan>());
            }

            // Record the ping and create new request id.
            var requestId = RequestCounter++;

            pings[peerId].Add(requestId, DateTime.UtcNow.TimeOfDay);

            return requestId;
        }

        public bool RecordPong(int peerId, ulong requestId)
        {
            // Make sure peer has active session.
            if (!samples.TryGetValue(peerId, out var sampleBuffer))
                return false;

            if (!pings[peerId].TryGetValue(requestId, out var pingTime))
                return false;

            // Remove the ping request from pending ones.
            pings[peerId].Remove(requestId);

            // Get pong time that is the current time.
            var pongTime = DateTime.UtcNow.TimeOfDay;

            // Remove oldest sample from the buffer if 
            if (sampleBuffer.Count + 1 >= maxSamples)
                sampleBuffer.Dequeue();

            sampleBuffer.Enqueue(pongTime - pingTime);

            return true;
        }

        public TimeSpan GetAverage(int peerId)
        {
            if (!samples.TryGetValue(peerId, out var peerSamples))
                return TimeSpan.Zero;

            if (peerSamples.Count < 3)
                return peerSamples.Count == 0 ? TimeSpan.Zero : TimeSpan.FromMilliseconds(peerSamples.Average(t => t.TotalMilliseconds));

            var max = GetMax(peerId);
            var min = GetMin(peerId);

            return TimeSpan.FromMilliseconds(peerSamples.Where(s => s > min && s < max).Average(t => t.TotalMilliseconds));
        }

        public TimeSpan GetMax(int peerId)
            => !samples.ContainsKey(peerId) ? TimeSpan.Zero : samples[peerId].Max();

        public TimeSpan GetMin(int peerId)
            => !samples.ContainsKey(peerId) ? TimeSpan.Zero : samples[peerId].Min();

        public void Reset(int peerId)
            => samples.Remove(peerId);
    }

    /// <summary>
    /// Script that provides echo updates and request handler for <see cref="EchoMessage"/>.
    /// </summary>
    public sealed class EchoControlScript : ApplicationScript
    {
        #region Fields
        private readonly ILatencyService        latency;
        private readonly IEventSchedulerService scheduler;
        #endregion

        [BindingConstructor]
        public EchoControlScript(IApplicationScriptingHost application, ILatencyService latency, IEventSchedulerService scheduler)
            : base(application)
        {
            this.latency   = latency ?? throw new ArgumentNullException(nameof(latency));
            this.scheduler = scheduler ?? throw new ArgumentNullException(nameof(latency));

            application.Join += Application_OnJoin;

            application.Requests.Router.Use(MessageMatch.Exact<EchoMessage>(), HandleEcho);
        }

        private void HandleEcho(IRequest request, IResponse response)
        {
            var message = (EchoMessage)request.Message;

            switch (message.Phase)
            {
                case EchoPhase.Ping:
                    Log.Debug($"received echo request from peer {request.Connection.PeerId}");

                    response.Ok(Message.Create<EchoMessage>(m =>
                    {
                        m.Phase     = EchoPhase.Pong;
                        m.RequestId = message.RequestId;
                    }));

                    break;
                case EchoPhase.Pong:
                    if (latency.RecordPong(request.Connection.PeerId, message.RequestId))
                    {
                        Log.Information(
                            $"updated peer {request.Connection.PeerId} latency metrics: min: {latency.GetMin(request.Connection.PeerId).TotalMilliseconds}ms, " +
                            $"average: {latency.GetAverage(request.Connection.PeerId).TotalMilliseconds}ms, " +
                            $"max: {latency.GetMax(request.Connection.PeerId).TotalMilliseconds}ms");

                        response.Ok();
                    }
                    else
                    {
                        Log.Warning($"got unexpected echo response from peer {request.Connection.PeerId}", request);

                        response.BadRequest();
                    }

                    break;
                default:
                    throw new InvalidOrUnsupportedException(nameof(EchoPhase), message.Phase);
            }
        }

        private void CreatePeerLatencyTestScheduler(int peerId)
            => scheduler.Pulse((args) =>
                               {
                                   if (!Application.PeerIds.Contains(peerId))
                                   {
                                       Log.Debug($"clearing peer {peerId} latency metrics");

                                       latency.Reset(peerId);

                                       return PulseEventResult.Break;
                                   }

                                   Log.Debug($"testing peer {peerId} latency...");

                                   Application.Notifications.Queue.Enqueue(n => n.Send(peerId,
                                                                                       Message.Create<EchoMessage>(m =>
                                                                                       {
                                                                                           m.RequestId = latency.RecordPing(peerId);
                                                                                       })));

                                   return PulseEventResult.Continue;
                               },
                               TimeSpan.FromSeconds(5));

        #region Event handlers
        private void Application_OnJoin(object sender, in PeerJoinEventArgs e)
            => CreatePeerLatencyTestScheduler(e.Connection.PeerId);
        #endregion
    }
}