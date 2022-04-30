using System;
using System.Collections.Generic;
using System.Linq;
using Fracture.Common.Di.Attributes;
using Fracture.Common.Events;
using Fracture.Engine.Core;

namespace Fracture.Engine.Events
{
   /// <summary>
   /// Interface for implementing event queue systems.
   /// </summary>
   public interface IEventQueueSystem : IGameEngineSystem
   {
      /// <summary>
      /// Creates shared event that accepts specific topic and args and returns the backlog to the caller.
      /// </summary>
      ISharedEvent<TTopic, TArgs> CreateShared<TTopic, TArgs>(int capacity = 64, 
                                                              LetterRetentionPolicy retentionPolicy = LetterRetentionPolicy.PublishDeletedTopics);

      /// <summary>
      /// Creates shared event that accepts specific topic and args and returns the backlog to the caller.
      /// </summary>
      IUniqueEvent<TTopic, TArgs> CreateUnique<TTopic, TArgs>(int capacity = 16, 
                                                              LetterRetentionPolicy retentionPolicy = LetterRetentionPolicy.PublishDeletedTopics);
      
      /// <summary>
      /// Gets the event handler for event queue that has specific letter signature.
      /// </summary>
      IEventHandler<TTopic, TArgs> GetEventHandler<TTopic, TArgs>();
   }

   /// <summary>
   /// Default implementation of <see cref="IEventQueueSystem"/>.
   /// </summary>
   public sealed class EventQueueSystem : GameEngineSystem, IEventQueueSystem
   {
      #region Fields
      private readonly List<IEventQueue> queues;
      #endregion

      [BindingConstructor]
      public EventQueueSystem()
         => queues = new List<IEventQueue>();
      
      public ISharedEvent<TTopic, TArgs> CreateShared<TTopic, TArgs>(int capacity, LetterRetentionPolicy retentionPolicy)
      {
         var backlog = new SharedEvent<TTopic, TArgs>(capacity, retentionPolicy);
         
         queues.Add(backlog);
         
         return backlog;
      }

      public IUniqueEvent<TTopic, TArgs> CreateUnique<TTopic, TArgs>(int capacity, LetterRetentionPolicy retentionPolicy)
      {
         var backlog = new UniqueEvent<TTopic, TArgs>(capacity, retentionPolicy);
         
         queues.Add(backlog);
         
         return backlog;
      }

      public IEventHandler<TTopic, TArgs> GetEventHandler<TTopic, TArgs>()
      {
         var handler = (IEventHandler<TTopic, TArgs>)queues.FirstOrDefault(q => q is IEventHandler<TTopic, TArgs>);

         if (handler == null)
            throw new InvalidOperationException($"could not find handler for topic <{typeof(TTopic).FullName}, {typeof(TArgs).FullName}>");
         
         return handler;
      }
      
      public override void Update(IGameEngineTime time)
      {
         foreach (var queue in queues)
            queue.Clear();
      }
   }
}