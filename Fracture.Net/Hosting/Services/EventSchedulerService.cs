using System;
using Fracture.Common;
using Fracture.Common.Di.Attributes;
using Fracture.Common.Events;
using Serilog;

namespace Fracture.Net.Hosting.Services
{
    public enum PulseEventResult : byte
    {
        Continue,
        Break
    }

    public delegate void ScheduledCallbackDelegate(ScheduledEventArgs args);

    public delegate PulseEventResult ScheduledPulseCallbackDelegate(ScheduledEventArgs args);

    public interface IEventSchedulerService : IApplicationService
    {
        void Signal(ScheduledCallbackDelegate callback, TimeSpan delay, object state = null);

        void Pulse(ScheduledPulseCallbackDelegate callback, TimeSpan interval, object state = null);
    }

    public sealed class EventSchedulerService : ActiveApplicationService, IEventSchedulerService
    {
        #region Fields
        private readonly EventScheduler scheduler;
        #endregion

        [BindingConstructor]
        public EventSchedulerService(IApplicationServiceHost application)
            : base(application)
        {
            scheduler = new EventScheduler();
        }

        public void Signal(ScheduledCallbackDelegate callback, TimeSpan delay, object state = null)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            var pulse = new SyncScheduledEvent(ScheduledEventType.Signal, state);

            pulse.Invoke += (s, args) =>
            {
                try
                {
                    callback(args);
                }
                catch (Exception e)
                {
                    Log.Error(e, "unhandled error when executing scheduled signal event");
                }
            };

            pulse.Wait(delay);

            scheduler.Add(pulse);
        }

        public void Pulse(ScheduledPulseCallbackDelegate callback, TimeSpan interval, object state = null)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            var pulse = new SyncScheduledEvent(ScheduledEventType.Pulse, state);

            pulse.Invoke += (s, args) =>
            {
                try
                {
                    var result = callback(args);

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