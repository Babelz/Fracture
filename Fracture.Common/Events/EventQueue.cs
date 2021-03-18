using System;
using System.Collections.Generic;
using Fracture.Common.Collections;

namespace Fracture.Common.Events
{
   /// <summary>
   /// Static class containing usage hints that define bucket size for event queues.
   /// </summary>
   public static class EventQueueUsageHint
   {
      #region Constant fields
      /// <summary>
      /// Queue is idle for most of the time containing few events at max. Suitable for queues with
      /// few topics and publishers.
      /// </summary>
      public const int Lazy = 8;
      
      /// <summary>
      /// Queue is being used by few publishers and has more than few events during one frame.
      /// </summary>
      public const int Moderate = 128;
      
      /// <summary>
      /// Queue is being published to often during a frame. Suitable when the queue has multiple topics
      /// and events are being posted frequently.
      /// </summary>
      public const int Normal = 256;
      
      /// <summary>
      /// Queue is being used heavily during a frame. Consumes more memory but can keep dispatching performance higher.
      /// </summary>
      public const int Frequent = 1024;
      #endregion
   }
   
   /// <summary>
   /// Interface for implementing events. This interface provides subscription related operations regarding events. 
   /// </summary>
   public interface IEvent<in TKey, in TSubscriber> where TSubscriber : Delegate
   {
      /// <summary>
      /// Subscribe handler to topic with given key.
      /// </summary>
      void Subscribe(TKey key, TSubscriber handler);

      /// <summary>
      /// Unsubscribe handler from topic with given key.
      /// </summary>
      void Unsubscribe(TKey key, TSubscriber handler);
   }
   
   /// <summary>
   /// Interface for implementing event dispatcher. Dispatchers dispatch all events queued to any event source at once.
   /// </summary>
   public interface IEventDispatcher
   {
      /// <summary>
      /// Dispatch all events in this event source.
      /// </summary>
      void Dispatch();
   }

   /// <summary>
   /// Interface for implementing event queues. Event queues allow creation of topics and subscribers can listen for
   /// events published to those topics. Event queues provide managed, predicable and delayed way of invoking events.
   /// </summary>
   public interface IEventQueue<in TKey, TSubscriber> : IEventDispatcher, IEvent<TKey, TSubscriber> 
                                                        where TSubscriber : Delegate
   {
      /// <summary>
      /// Returns boolean declaring whether topic with given key exists.
      /// </summary>
      bool Exists(TKey key);

      /// <summary>
      /// Creates new topic and associates given key with it. This allows subscriptions to be made to the topic.
      /// </summary>
      void Create(TKey key);

      /// <summary>
      /// Deletes topic with given key.
      /// </summary>
      bool Delete(TKey key);

      /// <summary>
      /// Publishes event to the queue.
      /// </summary>
      void Publish(TKey key, EventDispatcher<TSubscriber> dispatcher);
   }
   
   /// <summary>
   /// Delegate that is used to capture arguments that are passed to the event when it is invoked.
   /// </summary>
   public delegate void EventDispatcher<in T>(T handler) where T : Delegate;

   /// <summary>
   /// Struct that contains context for dispatching events from the queue to the actual subscribers.
   /// </summary>
   public readonly struct EventDispatchContext<TKey, TSubscriber> where TSubscriber : Delegate
   {
      #region Properties
      public TKey Key
      {
         get;
      }
         
      public EventDispatcher<TSubscriber> Dispatcher
      {
         get;
      }
      #endregion

      public EventDispatchContext(TKey key, EventDispatcher<TSubscriber> dispatcher)
      {
         Key        = key;
         Dispatcher = dispatcher;
      }
   }
   
   /// <summary>
   /// Event queue that supports generic event dispatch for multiple event sources and allows single topic to contain
   /// multiple events at any time.
   /// </summary>
   public class SharedEventQueue<TKey, TSubscriber> : IEventQueue<TKey, TSubscriber> where TSubscriber : Delegate
   {
      #region Fields
      private readonly LinearGrowthArray<EventDispatchContext<TKey, TSubscriber>> events;

      private readonly Dictionary<TKey, List<TSubscriber>> topics;
      
      private int count;
      #endregion

      public SharedEventQueue(int queueBucketSize)
      {
         events = new LinearGrowthArray<EventDispatchContext<TKey, TSubscriber>>(queueBucketSize);
         topics = new Dictionary<TKey, List<TSubscriber>>();
      }
      
      public bool Exists(TKey key)
         => topics.ContainsKey(key);
      
      public void Create(TKey key)
      {
         if (Exists(key)) 
            throw new InvalidOperationException($"event {key} already exists");
         
         topics.Add(key, new List<TSubscriber>());
      }
      
      public bool Delete(TKey key)
         => topics.Remove(key);

      public void Subscribe(TKey key, TSubscriber handler) 
         => topics[key].Add(handler);
      
      public void Unsubscribe(TKey key, TSubscriber handler)
      {
         if (!topics[key].Remove(handler)) 
            throw new InvalidOperationException("subscription not present in the topic");
      }
      
      public virtual void Publish(TKey key, EventDispatcher<TSubscriber> dispatcher)
      {
         if (!Exists(key))
            return;
  
         events.Insert(count++, new EventDispatchContext<TKey, TSubscriber>(key, dispatcher));
      }

      public virtual void Dispatch()
      {
         // No events, quick return.
         if (count == 0) 
            return;

         for (var i = 0; i < count; i++)
         {
            // Handle for each dispatch context.
            ref var context = ref events.AtIndex(i);
            
            // Check that this this topic still exists.
            if (!Exists(context.Key))
               continue;
         
            // Dispatch all events to subscriptions.
            var subscribers = topics[context.Key];
            
            for (var j = 0; j < subscribers.Count; j++)
               context.Dispatcher(subscribers[j]);
         }
         
         // Clear events.
         count = 0;
      }
   }

   /// <summary>
   /// Event queue that supports generic event dispatch for multiple event sources
   /// and allows single topic to contain one event at time.
   /// </summary>
   public sealed class UniqueEventQueue<TKey, TSubscriber> : SharedEventQueue<TKey, TSubscriber> where TSubscriber : Delegate
   {
      #region Fields
      private readonly HashSet<TKey> lookup;
      #endregion
      
      public UniqueEventQueue(int queueBucketSize)
         : base(queueBucketSize)
      {
         lookup = new HashSet<TKey>();
      }
      
      public override void Publish(TKey key, EventDispatcher<TSubscriber> dispatcher)
      {
         if (!Exists(key))
            return;
         
         if (!lookup.Add(key))
            return;
            
         base.Publish(key, dispatcher);
      }

      public override void Dispatch()
      {
         base.Dispatch();
         
         lookup.Clear();
      }
   }
}