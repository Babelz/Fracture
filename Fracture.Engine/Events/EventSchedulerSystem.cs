using Fracture.Common.Events;
using Fracture.Engine.Core;

namespace Fracture.Engine.Events
{
    /// <summary>
    /// Interface for implementing systems that provide basic event scheduling support
    /// for the game engine.
    /// </summary>
    public interface IEventSchedulerSystem : IActiveGameEngineSystem, IEventScheduler
    {
        // Nothing to implement. Union interface.
    }
    
    public sealed class EventSchedulerSystem : ActiveGameEngineSystem, IEventSchedulerSystem
    {
        #region Fields
        private readonly EventScheduler scheduler;
        #endregion
        
        public EventSchedulerSystem(int priority)
            : base(priority) => scheduler = new EventScheduler();

        public void Add(IScheduledEvent scheduledEvent)
            => scheduler.Add(scheduledEvent);

        public void Remove(IScheduledEvent scheduledEvent)
            => scheduler.Remove(scheduledEvent);

        public bool Exists(IScheduledEvent scheduledEvent)
            => scheduler.Exists(scheduledEvent);

        public void Clear()
            => scheduler.Clear();
        
        public override void Update(IGameEngineTime time)
            => scheduler.Tick(time.Elapsed);
    }
}
