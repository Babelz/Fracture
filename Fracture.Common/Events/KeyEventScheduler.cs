using System;
using System.Collections.Generic;
using System.Linq;

namespace Fracture.Common.Events
{
    /// <summary>
    /// Interface for implementing event schedulers
    /// that associate events with keys.
    /// </summary>
    public interface IKeyEventScheduler
    {
        IScheduledEvent GetEvent(object key);

        bool Exists(object key);

        void Add(IScheduledEvent scheduledEvent, object key);
        
        void Remove(object key);

        void Clear();
    }
    
    public class KeyEventScheduler : IKeyEventScheduler
    {
        #region Fields
        private readonly Dictionary<object, IScheduledEvent> pairs;
        #endregion

        public KeyEventScheduler()
            => pairs = new Dictionary<object, IScheduledEvent>();
      
        public IScheduledEvent GetEvent(object key)
            => pairs[key];

        public bool Exists(object key)
            => pairs.ContainsKey(key);
        
        public void Add(IScheduledEvent scheduledEvent, object key)
        {
            if (scheduledEvent == null)
                throw new ArgumentNullException(nameof(scheduledEvent));
            
            if (pairs.ContainsKey(key))
                throw new InvalidOperationException("scheduler with key already present");

            pairs.Add(key, scheduledEvent);
        }

        public void Remove(object key)
        {
            if (!pairs.Remove(key))
                throw new InvalidOperationException("could not remove scheduler");
        }
        
        public void Clear()
            => pairs.Clear();
         
        public void Tick(TimeSpan elapsed)
        {
            foreach (var pair in pairs.ToList())
            {
                var wasWaitingBeforeTick = pair.Value.Waiting;

                pair.Value.Tick(elapsed);

                if (!wasWaitingBeforeTick || pair.Value.Waiting) 
                    continue;
                
                if (pair.Value.Type == ScheduledEventType.Pulse) 
                    pair.Value.Wait(pair.Value.DueTime);
                else                                             
                    pairs.Remove(pair.Key);
            }
        }
    }
}
