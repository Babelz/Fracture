using System;
using Fracture.Common.Collections;

namespace Fracture.Engine.Core.Systems
{
    /// <summary>
    /// Interface for implementing systems that provide game time information and snapshots of past frames timestamps.
    /// </summary>
    public interface IGameTimeSystem : IGameEngineSystem
    {
        #region Properties
        IGameEngineTime Current
        {
            get;
        }
        #endregion
        
        IGameEngineTime Snapshot(int pastFrameIndex);
    }
    
    /// <summary>
    /// Default implementation of <see cref="IGameTimeSystem"/>.
    /// </summary>
    public class GameTimeSystem : GameEngineSystem, IGameTimeSystem
    {
        #region Game engine time snapshot struct
        private readonly struct GameEngineTimeSnapshot : IGameEngineTime
        {
            #region Properties
            public TimeSpan StartTime
            {
                get;
            }

            public TimeSpan Elapsed
            {
                get;
            }

            public TimeSpan Total
            {
                get;
            }

            public ulong Tick
            {
                get;
            }
            #endregion

            public GameEngineTimeSnapshot(IGameEngineTime time)
            {
                StartTime = time.StartTime;   
                Elapsed   = time.Elapsed;   
                Total     = time.Total;   
                Tick      = time.Tick;   
            }
        }
        #endregion
        
        #region Fields
        private readonly CircularBuffer<GameEngineTimeSnapshot> snapshots;
        #endregion
        
        #region Properties
        public IGameEngineTime Current
        {
            get;
        }
        #endregion

        public GameTimeSystem(IGameEngineTime time, int snapshotsCount = 60)
        {
            Current   = time;
            snapshots = new CircularBuffer<GameEngineTimeSnapshot>(60);
        }
        
        public IGameEngineTime Snapshot(int pastFrameIndex)
            => snapshots.AtOffset(pastFrameIndex);

        public override void Update(IGameEngineTime time)
            => snapshots.Push(new GameEngineTimeSnapshot(Current));
    }
}