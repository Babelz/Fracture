using System;
using System.Diagnostics;

namespace Fracture.Net.Hosting
{
    public interface IApplicationClock
    {
        #region Properties
        public TimeSpan Elapsed
        {
            get;
        }
        
        public TimeSpan Current
        {
            get;
        }
        
        public TimeSpan Total
        {
            get;
        }
        #endregion
    }
    
    public interface IApplicationTimer : IApplicationClock
    {
        void Tick();
    }
    
    public sealed class ApplicationTimer : IApplicationTimer
    {
        #region Fields
        private readonly Stopwatch timer;
        
        private TimeSpan elapsed;
        private TimeSpan total;
        #endregion

        #region Properties
        public TimeSpan Elapsed
            => elapsed;

        public TimeSpan Current
            => timer.Elapsed;

        public TimeSpan Total
            => total;
        #endregion

        public ApplicationTimer()
            => timer = Stopwatch.StartNew();
        
        public void Tick()
        {
            total   += timer.Elapsed;
            elapsed = timer.Elapsed;
            
            timer.Restart();
        }
    }
}