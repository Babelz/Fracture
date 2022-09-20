using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fracture.Net.Hosting;

namespace Fracture.Net.Tests.Util.Hosting.Fakes
{
    /// <summary>
    /// Structure that contains timer delta value for specific tick.
    /// </summary>
    public readonly struct FakeApplicationTimerStep
    {
        #region Properties
        /// <summary>
        /// Gets the tick value of this frame. When the current tick of the fake timer matches this value the frame should be applied.
        /// </summary>
        public ulong Tick
        {
            get;
        }

        /// <summary>
        /// Gets the delta value of this this frame. When the frame is considered active and should be applied, this delta value will be used during the tick.
        /// </summary>
        public TimeSpan Delta
        {
            get;
        }
        #endregion

        public FakeApplicationTimerStep(ulong tick, TimeSpan delta)
        {
            Tick  = tick;
            Delta = delta;
        }
    }
    
    /// <summary>
    /// Class that provides application timer for testing purposes. The delta value and the passing total time of the application can be manipulated by
    /// supplying list of <see cref="FakeApplicationTimerStep"/> values.
    /// </summary>
    public class FakeApplicationTimer : IApplicationTimer
    {
        #region Static fields
        /// <summary>
        /// Default delta value used for every tick that does not have active tick frame.
        /// </summary>
        public static readonly TimeSpan DefaultDelta = TimeSpan.FromMilliseconds(16);
        #endregion

        #region Fields
        private readonly TimeSpan                   delta;
        private readonly FakeApplicationTimerStep[] steps;
        #endregion
        
        #region Properties
        public TimeSpan Elapsed
        {
            get;
            private set;
        }

        public TimeSpan Current
        {
            get;
            private set;
        }

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

        public FakeApplicationTimer(TimeSpan delta, params FakeApplicationTimerStep[] steps)
        {
            this.delta = delta;
            this.steps = steps ?? throw new ArgumentNullException(nameof(steps));
        }
        
        public FakeApplicationTimer(params FakeApplicationTimerStep[] steps)
            : this(DefaultDelta, steps)
        {
        }

        public FakeApplicationTimer(TimeSpan delta)
            : this(delta, Array.Empty<FakeApplicationTimerStep>())
        {
        }
        
        public void Tick()
        {
            var activeDelta = delta;

            if (steps.Length != 0)
            {
                var fakeStep = steps.FirstOrDefault(s => s.Tick == Ticks);

                if (fakeStep.Delta != TimeSpan.Zero)
                    activeDelta = fakeStep.Delta;
            }
            
            Ticks++;

            Total   += activeDelta;
            Elapsed =  activeDelta;
            Current =  activeDelta;
        }
    }
}