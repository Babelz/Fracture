using System;
using System.Diagnostics;

namespace Fracture.Common.Runtime
{
    public sealed class ExecutionTimer
    {
        #region Fields
        private readonly Stopwatch timer;

        private readonly TimeSpan samplingTime;

        private TimeSpan lastResetTimestamp;
        private TimeSpan lastMeasurementTimestamp;

        private int samplesCount;
        #endregion

        #region Properties
        public string Name
        {
            get;
        }

        public TimeSpan Min
        {
            get;
            private set;
        }

        public TimeSpan Max
        {
            get;
            private set;
        }

        public TimeSpan Average
        {
            get;
            private set;
        }

        public bool Ready => (lastMeasurementTimestamp - lastResetTimestamp) >= samplingTime;
        #endregion

        public ExecutionTimer(string name, TimeSpan samplingTime)
        {
            Name  = !string.IsNullOrEmpty(name) ? name : throw new ArgumentNullException(nameof(name));
            timer = new Stopwatch();

            this.samplingTime = samplingTime > TimeSpan.Zero
                ? samplingTime
                : throw new ArgumentOutOfRangeException(nameof(samplingTime), "expecting positive non-zero time");

            Reset();
        }

        public void Reset()
        {
            lastResetTimestamp       = DateTime.UtcNow.TimeOfDay;
            lastMeasurementTimestamp = DateTime.UtcNow.TimeOfDay;

            Min     = TimeSpan.MaxValue;
            Max     = TimeSpan.MinValue;
            Average = TimeSpan.Zero;
        }

        public void Begin()
        {
            if (Ready)
                return;

            timer.Restart();
        }

        public void End(Action readyCallback = null)
        {
            if (Ready)
                return;

            timer.Stop();

            lastMeasurementTimestamp = DateTime.UtcNow.TimeOfDay;

            samplesCount++;

            var elapsed = timer.Elapsed;

            if (elapsed < Min)
                Min = elapsed;
            else if (elapsed > Max)
                Max = elapsed;

            Average -= TimeSpan.FromTicks(Average.Ticks / samplesCount);
            Average += TimeSpan.FromTicks(elapsed.Ticks / samplesCount);

            if (!Ready || readyCallback == null)
                return;

            readyCallback();

            Reset();
        }
    }
}