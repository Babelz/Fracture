using System;
using System.Collections.Generic;

namespace Fracture.Engine.Core.Components
{
    /// <summary>
    /// Class that provides time information about state changes of an object.
    /// </summary>
    public sealed class StateWatcher<T>
    {
        #region Fields
        private readonly Dictionary<T, TimeSpan> activityTimers;
        private readonly Dictionary<T, TimeSpan> inactivityTimers;
        #endregion

        public StateWatcher()
        {
            activityTimers   = new Dictionary<T, TimeSpan>();
            inactivityTimers = new Dictionary<T, TimeSpan>();
        }

        public TimeSpan TimeActive(T state)
        {
            activityTimers.TryGetValue(state, out var time);

            return time;
        }

        public TimeSpan TimeInactive(T state)
        {
            inactivityTimers.TryGetValue(state, out var time);

            return time;
        }

        public void Update(IGameEngineTime time, IEnumerable<T> inactiveStates, IEnumerable<T> activeStates)
        {
            foreach (var inactiveState in inactiveStates)
            {
                if (!inactivityTimers.ContainsKey(inactiveState))
                    inactivityTimers.Add(inactiveState, TimeSpan.Zero);

                inactivityTimers[inactiveState] += time.Elapsed;

                activityTimers.Remove(inactiveState);
            }

            foreach (var activeState in activeStates)
            {
                if (!activityTimers.ContainsKey(activeState))
                    activityTimers.Add(activeState, TimeSpan.Zero);

                activityTimers[activeState] += time.Elapsed;

                inactivityTimers.Remove(activeState);
            }
        }
    }
}