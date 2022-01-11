using System.Collections.Generic;
using System.Linq;
using Fracture.Common.Di.Attributes;
using Fracture.Common.Events;
using Fracture.Engine.Core;

namespace Fracture.Engine.Events
{
   public interface IEventQueueSystem : IGameEngineSystem
   {
      ISharedEvent<TTopic, TArgs> CreateSharedEvent<TTopic, TArgs>(int capacity = 64, 
                                                                   EventRetentionPolicy retentionPolicy = EventRetentionPolicy.PublishDeletedTopics);

      IUniqueEvent<TTopic, TArgs> CreateUniqueEvent<TTopic, TArgs>(int capacity = 16, 
                                                                   EventRetentionPolicy retentionPolicy = EventRetentionPolicy.PublishDeletedTopics);
      
      IEventHandler<TTopic, TArgs> GetEventHandler<TTopic, TArgs>();
   }

   public sealed class EventQueueSystem : GameEngineSystem, IEventQueueSystem
   {
      #region Fields
      private readonly List<IEventQueue> queues;
      #endregion

      [BindingConstructor]
      public EventQueueSystem()
         => queues = new List<IEventQueue>();
      
      public ISharedEvent<TTopic, TArgs> CreateSharedEvent<TTopic, TArgs>(int capacity, EventRetentionPolicy retentionPolicy)
      {
         var backlog = new SharedEvent<TTopic, TArgs>(capacity, retentionPolicy);
         
         queues.Add(backlog);
         
         return backlog;
      }

      public IUniqueEvent<TTopic, TArgs> CreateUniqueEvent<TTopic, TArgs>(int capacity, EventRetentionPolicy retentionPolicy)
      {
         var backlog = new UniqueEvent<TTopic, TArgs>(capacity, retentionPolicy);
         
         queues.Add(backlog);
         
         return backlog;
      }

      public IEventHandler<TTopic, TArgs> GetEventHandler<TTopic, TArgs>()
         => (IEventHandler<TTopic, TArgs>)queues.FirstOrDefault(q => q is IEventHandler<TTopic, TArgs>) ?? EventBacklog<TTopic, TArgs>.Empty;
      
      public override void Update(IGameEngineTime time)
      {
         foreach (var queue in queues)
            queue.Clear();
      }
   }
}