using System.Collections.Generic;
using System.Linq;
using Fracture.Common.Di.Attributes;
using Fracture.Common.Events;
using Fracture.Engine.Core;

namespace Fracture.Engine.Events
{
   public interface IEventQueueSystem : IGameEngineSystem
   {
      ISharedEventPublisher<TTopic, TArgs> CreateShared<TTopic, TArgs>(int capacity = 64);

      IUniqueEventPublisher<TTopic, TArgs> CreateUnique<TTopic, TArgs>(int capacity = 16);
      
      IEventHandler<TTopic, TArgs> GetHandler<TTopic, TArgs>();
   }

   public sealed class EventQueueSystem : GameEngineSystem, IEventQueueSystem
   {
      #region Fields
      private readonly List<IEventQueue> queues;
      #endregion

      [BindingConstructor]
      public EventQueueSystem()
         => queues = new List<IEventQueue>();
      
      public ISharedEventPublisher<TTopic, TArgs> CreateShared<TTopic, TArgs>(int capacity)
      {
         var backlog = new SharedEventBacklog<TTopic, TArgs>(capacity);
         
         queues.Add(backlog);
         
         return backlog;
      }

      public IUniqueEventPublisher<TTopic, TArgs> CreateUnique<TTopic, TArgs>(int capacity)
      {
         var backlog = new UniqueEventBacklog<TTopic, TArgs>(capacity);
         
         queues.Add(backlog);
         
         return backlog;
      }

      public IEventHandler<TTopic, TArgs> GetHandler<TTopic, TArgs>()
         => (IEventHandler<TTopic, TArgs>)queues.First(q => q is IEventHandler<TTopic, TArgs>);
      
      public override void Update(IGameEngineTime time)
      {
         foreach (var queue in queues)
            queue.Clear();
      }
   }
}