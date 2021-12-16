using System;
using Fracture.Common;
using Fracture.Common.Di.Attributes;
using Fracture.Common.Events;
using NLog;

namespace Fracture.Net.Hosting.Services
{
    public enum PulseEventResult : byte
    {
        Continue,
        Break
    }

    public delegate void ScheduledCallbackDelegate();
    
    public delegate PulseEventResult ScheduledPulseCallbackDelegate();

    public interface IEventSchedulerService : IApplicationService
    {
        void Signal(ScheduledCallbackDelegate callback, TimeSpan delay);
        
        void Pulse(ScheduledPulseCallbackDelegate callback, TimeSpan interval);
    }
    
    public sealed class EventSchedulerService : ActiveApplicationService, IEventSchedulerService
    {
        #region Static fields
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion
        
        #region Fields
        private readonly EventScheduler scheduler;
        #endregion

        [BindingConstructor]
        public EventSchedulerService(IApplicationServiceHost application) 
            : base(application)
        {
            scheduler = new EventScheduler();
        }
        
        public void Signal(ScheduledCallbackDelegate callback, TimeSpan delay)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));
            
            var pulse = new SyncScheduledEvent(ScheduledEventType.Signal);
            
            pulse.Invoke += delegate
            {
                try
                {
                    callback();
                }
                catch (Exception e)
                {
                    Log.Error(e, "unhandled error when executing scheduled signal event");
                }
            };
            
            pulse.Wait(delay);
            
            scheduler.Add(pulse);
        }

        public void Pulse(ScheduledPulseCallbackDelegate callback, TimeSpan interval)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));
            
            var pulse = new SyncScheduledEvent(ScheduledEventType.Pulse);
            
            pulse.Invoke += delegate
            {
                try
                {
                    var result = callback();
                    
                    switch (result)
                    {
                        case PulseEventResult.Continue:
                            break;
                        case PulseEventResult.Break:
                            scheduler.Remove(pulse);
                            break;
                        default:
                            throw new InvalidOrUnsupportedException(nameof(ScheduledEventType), result);
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e, "unhandled error when executing scheduled pulse event");
                    
                    scheduler.Remove(pulse);
                }
            };
            
            pulse.Wait(interval);
            
            scheduler.Add(pulse);
        }
        
        public override void Tick()
            => scheduler.Tick(Application.Clock.Elapsed);
    }
}