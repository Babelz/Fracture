using System;
using System.Collections.Generic;
using System.Linq;
using Fracture.Common;
using Fracture.Common.Collections;
using Fracture.Common.Di.Attributes;
using Fracture.Net.Hosting.Messaging;
using Fracture.Net.Hosting.Servers;
using Fracture.Net.Messages;
using NLog;

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
        ulong RecordPing(int id);
        
        /// <summary>
        /// Records pong timestamp for peer with given id. Returns boolean declaring whether request pair was found.
        /// </summary>
        bool RecordPong(int id, ulong requestId);

        TimeSpan GetAverage(int id);
        TimeSpan GetMax(int id);
        TimeSpan GetMin(int id);
        
        /// <summary>
        /// Resets latency metrics for peer with given id.
        /// </summary>
        void Reset(int id);
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

        public ulong RecordPing(int id)
        {
            // Create latency "session" if it does not exist.
            if (!samples.ContainsKey(id))
            {
                samples.Add(id, new Queue<TimeSpan>());
                
                pings.Add(id, new Dictionary<ulong, TimeSpan>());
            }
            
            // Record the ping and create new request id.
            var requestId = RequestCounter++;
            
            pings[id].Add(requestId, DateTime.UtcNow.TimeOfDay);
            
            return requestId;
        }

        public bool RecordPong(int id, ulong requestId)
        {
            // Make sure peer has active session.
            if (!samples.TryGetValue(id, out var sampleBuffer))
                return false;
            
            if (!pings[id].TryGetValue(requestId, out var pingTime))
                return false;
            
            // Remove the ping request from pending ones.
            pings[id].Remove(requestId);
            
            // Get pong time that is the current time.
            var pongTime = DateTime.UtcNow.TimeOfDay;
            
            // Remove oldest sample from the buffer if 
            if (sampleBuffer.Count + 1 >= maxSamples)
                sampleBuffer.Dequeue();
            
            sampleBuffer.Enqueue(pongTime - pingTime);
            
            return true;
        }

        public TimeSpan GetAverage(int id)
            => TimeSpan.FromMilliseconds(samples[id].Average(t => t.TotalMilliseconds));

        public TimeSpan GetMax(int id)
            => samples[id].Max();

        public TimeSpan GetMin(int id)
            => samples[id].Min();

        public void Reset(int id)
            => samples.Remove(id);
    }

    /// <summary>
    /// Script that provides echo updates and request handler for <see cref="EchoMessage"/>.
    /// </summary>
    public sealed class EchoControlScript : ApplicationScript
    {
        #region Static fields
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion
        
        #region Fields
        private readonly ILatencyService latency;
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
                    Log.Info($"received echo request from peer {request.Peer.Id}");
                    
                    response.Ok(Message.Take<EchoMessage>(m =>
                    {
                        m.Phase     = EchoPhase.Pong;
                        m.RequestId = message.RequestId;
                    }));
                    break;
                case EchoPhase.Pong:
                    if (latency.RecordPong(request.Peer.Id, message.RequestId))
                        response.Ok();
                    else
                    {
                        Log.Warn($"got unexpected echo response from peer {request.Peer.Id}", request);
                        
                        response.BadRequest();
                    }
                    break;
                default:
                    throw new InvalidOrUnsupportedException(nameof(EchoPhase), message.Phase);
            }
        }
        
        private void CreatePeerLatencyTestScheduler(int id)
        {
            scheduler.Pulse(() =>
            {
                if (!Application.Peers.Contains(id))
                    return PulseEventResult.Break;

                Application.Notifications.Queue.Enqueue().Send(id, Message.Take<EchoMessage>(m =>
                {
                    m.RequestId = latency.RecordPing(id);
                }));
                
                return PulseEventResult.Continue;
            }, TimeSpan.FromSeconds(5));
        }

        #region Event handlers
        private void Application_OnJoin(object sender, in PeerJoinEventArgs e)
            => CreatePeerLatencyTestScheduler(e.Peer.Id);
        #endregion
    }
}