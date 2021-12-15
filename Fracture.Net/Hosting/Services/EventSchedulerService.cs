using Fracture.Common.Events;

namespace Fracture.Net.Hosting.Services
{
    public interface IEventSchedulerService : IApplicationService
    {
        #region Properties
        IKeyEventScheduler KeyEvents
        {
            get;
        }
        
        IEventScheduler FreeEvents
        {
            get;
        }
        #endregion
    }
    
    public sealed class EventSchedulerService : ActiveApplicationService, IEventSchedulerService
    {
        #region Fields
        private readonly EventScheduler freeEvents;
        
        private readonly KeyEventScheduler keyEvents;
        #endregion

        #region Properties
        public IKeyEventScheduler KeyEvents
            => keyEvents;

        public IEventScheduler FreeEvents
            => freeEvents;
        #endregion
        
        public EventSchedulerService(IApplicationServiceHost application) 
            : base(application)
        {
            freeEvents = new EventScheduler();
            keyEvents  = new KeyEventScheduler();
        }

        public override void Tick()
        {
            freeEvents.Tick(Application.Clock.Elapsed);
            keyEvents.Tick(Application.Clock.Elapsed);
        }
    }
}