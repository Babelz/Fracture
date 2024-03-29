using System;
using System.Diagnostics;

namespace Fracture.Net.Hosting
{
    /// <summary>
    /// Interface for implementing application specific clocks that are used for tracking time inside the application between tick frames.
    /// </summary>
    public interface IApplicationClock
    {
        #region Properties
        /// <summary>
        /// Gets the time it took to run the last tick frame.
        /// </summary>
        public TimeSpan Elapsed
        {
            get;
        }

        /// <summary>
        /// Gets the current time that has passed since the start of the tick frame.
        /// </summary>
        public TimeSpan Current
        {
            get;
        }

        /// <summary>
        /// Gets the total time elapsed since application was started.
        /// </summary>
        public TimeSpan Total
        {
            get;
        }

        /// <summary>
        /// Gets the total count of application ticks that have passed since start.
        /// </summary>
        ulong Ticks
        {
            get;
        }
        #endregion
    }

    /// <summary>
    /// Interface that provides timing services for tracking the application clock.
    /// </summary>
    public interface IApplicationTimer : IApplicationClock
    {
        /// <summary>
        /// Creates new tick frame and starts measuring time for it.
        /// </summary>
        void Tick();
    }

    /// <summary>
    /// Default implementation of <see cref="IApplicationTimer"/>.
    /// </summary>
    public sealed class ApplicationTimer : IApplicationTimer
    {
        #region Fields
        private readonly Stopwatch timer;
        #endregion

        #region Properties
        public TimeSpan Elapsed
        {
            get;
            private set;
        }

        public TimeSpan Current => timer.Elapsed;

        public TimeSpan Total
        {
            get;
            private set;
        }

        public ulong Ticks
        {
            get;
            private set;
        }
        #endregion

        public ApplicationTimer()
            => timer = Stopwatch.StartNew();

        public void Tick()
        {
            Ticks++;

            Total   += timer.Elapsed;
            Elapsed =  timer.Elapsed;

            timer.Restart();
        }
    }
}