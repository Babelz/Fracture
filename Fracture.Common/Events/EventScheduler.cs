using System;
using System.Collections.Generic;

namespace Fracture.Common.Events
{
    /// <summary>
    /// Interface for implementing event schedulers that handle and manage multiple scheduled events.
    /// </summary>
    public interface IEventScheduler
    {
        bool Exists(IScheduledEvent scheduledEvent);

        void Add(IScheduledEvent scheduledEvent);

        void Remove(IScheduledEvent scheduledEvent);

        void Clear();
    }
    
    public class EventScheduler : IEventScheduler
    {
        #region Fields
        private readonly List<IScheduledEvent> scheduledEvents;
        #endregion

        public EventScheduler()
            => scheduledEvents = new List<IScheduledEvent>();
        
        public bool Exists(IScheduledEvent scheduledEvent)
            => scheduledEvents.Contains(scheduledEvent);
        
        public void Add(IScheduledEvent scheduledEvent)
        {
            if (scheduledEvent == null)
                throw new ArgumentNullException(nameof(scheduledEvent));

            if (scheduledEvents.Contains(scheduledEvent))
                throw new InvalidOperationException("scheduler already present");

            scheduledEvents.Add(scheduledEvent);
        }

        public void Remove(IScheduledEvent scheduledEvent)
        {
            if (!scheduledEvents.Remove(scheduledEvent))
                throw new InvalidOperationException("could not remove scheduler");
        }
        
        public void Clear()
            => scheduledEvents.Clear();   
         
        public void Tick(TimeSpan elapsed)
        {
            var i = 0;

            while (i < scheduledEvents.Count)
            {
                var scheduledEvent       = scheduledEvents[i];
                var wasRunningBeforeTick = scheduledEvent.Waiting;
                
                scheduledEvent.Tick(elapsed);

                if (wasRunningBeforeTick && !scheduledEvent.Waiting)
                {
                    if (scheduledEvent.Type == ScheduledEventType.Pulse)
                    {
                        scheduledEvent.Wait();
                    }
                    else
                    {
                        // Continue from current index as the event was removed.
                        scheduledEvents.RemoveAt(i);

                        continue;
                    }
                }

                // Advance as no events was removed.
                i++;
            }
        }
    }
}
