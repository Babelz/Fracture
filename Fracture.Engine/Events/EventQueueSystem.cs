using System;
using System.Collections.Generic;
using Fracture.Common.Di.Attributes;
using Fracture.Common.Events;
using Fracture.Engine.Core;

namespace Fracture.Engine.Events
{
   /// <summary>
   /// Interface for implementing event queue systems.
   /// </summary>
   public interface IEventQueueSystem : IObjectManagementSystem
   {
      /// <summary>
      /// Creates new shared event queue and returns it to the caller.
      /// </summary>
      IEventQueue<TKey, TArgs> CreateShared<TKey, TArgs>();

      /// <summary>
      /// Creates new unique event queue and returns it to the caller.
      /// </summary>
      IEventQueue<TKey, TArgs> CreateUnique<TKey, TArgs>();

      /// <summary>
      /// Deletes given event queue.
      /// </summary>
      bool Delete<TKey, TEvent>(IEventQueue<TKey, TEvent> queue);
   }
   
   /// <summary>
   /// Game engine system that manages <see cref="SharedEventQueue{TKey,TSubscriber}"/> handles invoking events added
   /// to event queues in managed manner.
   /// </summary>
   public sealed class EventQueueSystem : GameEngineSystem, IEventQueueSystem
   {
      #region Fields
      private readonly List<IEventDispatcher> queues;
      #endregion

      [BindingConstructor]
      public EventQueueSystem()
         => queues = new List<IEventDispatcher>();

      public IEventQueue<TKey, TArgs> CreateShared<TKey, TArgs>()
      {
         var queue = new SharedEventQueue<TKey, TArgs>();
         
         queues.Add(queue);
         
         return queue;
      }
      
      public IEventQueue<TKey, TArgs> CreateUnique<TKey, TArgs>()
      {
         var queue = new UniqueEventQueue<TKey, TArgs>();
         
         queues.Add(queue);
         
         return queue;
      }

      public bool Delete<TKey, TArgs>(IEventQueue<TKey, TArgs> queue)
         => queues.Remove(queue);
      
      public void Clear()
         => queues.Clear();
      
      public override void Update(IGameEngineTime time)
      {
         foreach (var queue in queues)
            queue.Dispatch();
      }
   }
}