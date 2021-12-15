using System;
using System.Net.NetworkInformation;
using System.ServiceModel.Channels;
using Fracture.Common.Di.Attributes;
using Fracture.Common.Events;
using Fracture.Net.Hosting.Servers;
using Fracture.Net.Messages;
using Message = Fracture.Net.Messages.Message;

namespace Fracture.Net.Hosting.Services
{
    /// <summary>
    /// Interface for implementing services that provide latency recording of peers.
    /// </summary>
    public interface ILatencyMonitoringService : IApplicationService
    {
        /// <summary>
        /// Records ping timestamp for peer with given id.
        /// </summary>
        ulong RecordPing(int id);
        
        /// <summary>
        /// Records pong timestamp for peer with given id.
        /// </summary>
        void RecordPong(int id, ulong requestId);

        TimeSpan GetAverage(int id);
        TimeSpan GetMax(int id);
        TimeSpan GetMin(int id);
        
        /// <summary>
        /// Resets latency metrics for peer with given id.
        /// </summary>
        void Reset(int id);
    }
    
    public sealed class LatencyMonitoringScript : ApplicationScript
    {
        [BindingConstructor]
        public LatencyMonitoringScript(IApplicationScriptingHost application, ILatencyMonitoringService latency, IEventSchedulerService scheduler, int id) 
            : base(application)
        {
            var testPeerLatency = new SyncScheduledEvent(ScheduledEventType.Pulse);
            
            void TestPeerLatency(object sender, EventArgs args)
            {
                application.Notifications.Queue.Enqueue().Send(id, Message.Take<PingMessage>(m =>
                {
                    m.RequestId = latency.RecordPing(id);
                }));
            }
            
            testPeerLatency.Invoke += TestPeerLatency;
            
            void ResetLatencyMetrics(object sender, in PeerResetEventArgs args)
            {
                if (args.Peer.Id != id) 
                    return;
                
                latency.Reset(id);
                
                application.Reset -= ResetLatencyMetrics;
                
                scheduler.FreeEvents.Remove(testPeerLatency);
                
                testPeerLatency.Suspend();
            }
            
            application.Reset += ResetLatencyMetrics;
            
            application.Requests.Router.Use(MessageMatch.Exact<PingMessage>(), (request, response) =>
            {
                latency.RecordPong(id, ((PingMessage)request.Message).RequestId);
                
                response.Ok(); 
            });
            
            scheduler.FreeEvents.Add(testPeerLatency);
            
            testPeerLatency.Wait(TimeSpan.FromSeconds(5));
        }
    }
}