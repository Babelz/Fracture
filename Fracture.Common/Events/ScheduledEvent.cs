﻿using System;
using System.Threading.Tasks;

namespace Fracture.Common.Events
{
    /// <summary>
    /// Enumeration defining scheduled event types and how
    /// the events should behave.
    /// </summary>
    public enum ScheduledEventType : byte 
    {
        /// <summary>
        /// Event invokes the callback once.
        /// </summary>
        Signal = 0,

        /// <summary>
        /// Event keeps invoking the callback in given intervals.
        /// </summary>
        Pulse
    }
    
    /// <summary>
    /// Interface for implementing scheduled events. See <see cref="ScheduledEventType"/>
    /// how events behave.
    /// </summary>
    public interface IScheduledEvent
    {
        #region Events
        event EventHandler Invoke;
        #endregion

        #region Properties
        ScheduledEventType Type
        {
            get;
        }

        TimeSpan DueTime
        {
            get;
        }
        TimeSpan ElapsedTime
        {
            get;
        }

        bool Waiting
        {
            get;
        }
        #endregion

        /// <summary>
        /// Begins waiting to invoke the event.
        /// </summary>
        void Wait(TimeSpan dueTime);

        /// <summary>
        /// Begins waiting to invoke the event using last interval.
        /// </summary>
        void Wait();
        
        /// <summary>
        /// Resumes waiting for the event.
        /// </summary>
        void Resume();

        /// <summary>
        /// Suspends waiting for the event.
        /// </summary>
        void Suspend();

        void Tick(TimeSpan elapsed);
    }
        
    /// <summary>
    /// Scheduled event that runs asynchronously.
    /// </summary>
    public sealed class AsyncScheduledEvent : IScheduledEvent
    {
        #region Events
        public event EventHandler Invoke;
        #endregion

        #region Properties
        public ScheduledEventType Type
        {
            get;
        }

        public TimeSpan DueTime
        {
            get;
            private set;
        }

        public TimeSpan ElapsedTime
        {
            get;
            private set;
        }

        public bool Waiting
        {
            get;
            private set;
        }
        #endregion

        public AsyncScheduledEvent(ScheduledEventType type)
            => Type = type;

        public void Resume()
        {
            if (!Waiting)
                throw new InvalidOperationException("already running");

            if (DueTime == TimeSpan.Zero)
                throw new InvalidOperationException("can't resume, due time not specified");
            
            Waiting = true;
        }

        public void Wait(TimeSpan dueTime)
        {
            if (Waiting)
                throw new InvalidOperationException("already running");

            if (dueTime == TimeSpan.Zero)
                throw new InvalidOperationException("due time not specified");
            
            DueTime     = dueTime;
            ElapsedTime = TimeSpan.Zero;
            Waiting     = true;
        }

        public void Wait()
            => Wait(DueTime);

        public void Suspend()
        {
            if (!Waiting)
                throw new InvalidOperationException("not running");

            Waiting = false;
        }

        public void Tick(TimeSpan elapsed)
        {
            if (!Waiting) return;

            if (ElapsedTime < DueTime)
            {
                ElapsedTime += elapsed;    
                
                return;
            }
            
            Task.Factory.StartNew(() => Invoke?.Invoke(this, EventArgs.Empty));
            
            Waiting = false;
        }
    }
    
    public class SyncScheduledEvent : IScheduledEvent
    {
        #region Events
        public event EventHandler Invoke;
        #endregion

        #region Properties
        public ScheduledEventType Type
        {
            get;
        }

        public TimeSpan DueTime
        {
            get;
            private set;
        }

        public TimeSpan ElapsedTime
        {
            get;
            private set;
        }

        public bool Waiting
        {
            get;
            private set;
        }
        #endregion

        public SyncScheduledEvent(ScheduledEventType type)
            => Type = type;

        public void Resume()
        {
            if (Waiting)
                throw new InvalidOperationException("already running");

            if (DueTime == TimeSpan.Zero)
                throw new InvalidOperationException("can't resume, due time not specified");
            
            Waiting = true;
        }

        public void Wait(TimeSpan dueTime)
        {
            if (Waiting)
                throw new InvalidOperationException("already running");

            if (dueTime <= TimeSpan.Zero)
                throw new InvalidOperationException("due time not specified");

            DueTime     = dueTime;
            Waiting     = true;
            ElapsedTime = TimeSpan.Zero;
        }

        public void Wait()
            => Wait(DueTime);

        public void Suspend()
        {
            if (!Waiting)
                throw new InvalidOperationException("not running");

            Waiting = false;
        }

        public void Tick(TimeSpan elapsed)
        {
            if (!Waiting) return;

            ElapsedTime += elapsed;

            if (ElapsedTime < DueTime) return;
            
            Waiting = false;

            Invoke?.Invoke(this, EventArgs.Empty);
        }
    }
}
