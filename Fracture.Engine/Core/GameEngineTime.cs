using System;

namespace Fracture.Engine.Core
{
    /// <summary>
    /// Interface for implementing objects that contain time
    /// information about the engine.
    /// </summary>
    public interface IGameEngineTime
    {
        #region Properties
        /// <summary>
        /// Gets the start time of the engine in UTC.
        /// </summary>
        TimeSpan StartTime
        {
            get;
        }
        
        /// <summary>
        /// Gets the elapsed time from last frame.
        /// </summary>
        TimeSpan Elapsed
        {
            get;
        }

        /// <summary>
        /// Gets the time elapsed from the start.
        /// </summary>
        TimeSpan Total
        {
            get;
        }
        
        /// <summary>
        /// Gets the current frame number.
        /// </summary>
        ulong Tick
        {
            get;
        }
        #endregion
    }
    
    /// <summary>
    /// Class implementation of <see cref="IGameEngineTime"/>. Mutable by design
    /// to allow reuse of the same object between frames.
    /// </summary>
    public sealed class GameEngineTime : IGameEngineTime
    {
        #region Properties
        public TimeSpan StartTime
        {
            get;
        }
        
        public TimeSpan Elapsed
        {
            get;
            set;
        }

        public TimeSpan Total
        {
            get;
            set;
        }

        public ulong Tick
        {
            get;
            set;
        }
        #endregion

        public GameEngineTime()
        {
            StartTime = DateTime.UtcNow.TimeOfDay;
        }
    }
}