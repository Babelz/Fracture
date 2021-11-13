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
      IEventQueue<TKey, TEvent> CreateShared<TKey, TEvent>(int queueBucketSize) where TEvent : Delegate;

      /// <summary>
      /// Creates new unique event queue and returns it to the caller.
      /// </summary>
      IEventQueue<TKey, TEvent> CreateUnique<TKey, TEvent>(int queueBucketSize) where TEvent : Delegate;

      /// <summary>
      /// Deletes given event queue.
      /// </summary>
      bool Delete<TKey, TEvent>(IEventQueue<TKey, TEvent> queue) where TEvent : Delegate;
   }
   
   /// <summary>
   /// Game engine system that manages <see cref="SharedEventQueue{TKey,TSubscriber}"/> handles invoking events added
   /// to event queues in managed manner.
   /// </summary>
   public sealed class EventQueueSystem : ActiveGameEngineSystem, IEventQueueSystem
   {
      #region Fields
      private readonly List<IEventDispatcher> queues;
      #endregion

      [BindingConstructor]
      public EventQueueSystem(IGameEngine engine, int priority)
         : base(engine, priority) => queues = new List<IEventDispatcher>();

      public IEventQueue<TKey, TEvent> CreateShared<TKey, TEvent>(int queueBucketSize) where TEvent : Delegate
      {
         var queue = new SharedEventQueue<TKey, TEvent>(queueBucketSize);
         
         queues.Add(queue);
         
         return queue;
      }
      
      public IEventQueue<TKey, TEvent> CreateUnique<TKey, TEvent>(int queueBucketSize) where TEvent : Delegate
      {
         var queue = new UniqueEventQueue<TKey, TEvent>(queueBucketSize);
         
         queues.Add(queue);
         
         return queue;
      }

      public bool Delete<TKey, TEvent>(IEventQueue<TKey, TEvent> queue) where TEvent : Delegate
         => queues.Remove(queue);
      
      public void Clear()
         => queues.Clear();
      
      public override void Update()
      {
         foreach (var queue in queues)
            queue.Dispatch();
      }
   }
}