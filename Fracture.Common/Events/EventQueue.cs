using System;
using System.Collections.Generic;
using System.Linq;
using Fracture.Common.Collections;
using Fracture.Common.Memory.Pools;
using Fracture.Common.Memory.Storages;

namespace Fracture.Common.Events
{
   public delegate void EventCallbackDelegate<T>(in T args);

   /// <summary>
   /// Interface for implementing events. This interface provides subscription related operations regarding events. 
   /// </summary>
   public interface IEvent<in TKey, TArgs>
   {
      /// <summary>
      /// Subscribe handler to topic with given key.
      /// </summary>
      void Subscribe(TKey key, EventCallbackDelegate<TArgs> callback);

      /// <summary>
      /// Unsubscribe handler from topic with given key.
      /// </summary>
      void Unsubscribe(TKey key, EventCallbackDelegate<TArgs> callback);
      
      /// <summary>
      /// Returns boolean declaring whether topic with given key exists.
      /// </summary>
      bool Exists(TKey key);
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
   public interface IEventQueue<in TKey, TArgs> : IEventDispatcher, IEvent<TKey, TArgs>
   {
      /// <summary>
      /// Creates new topic and associates given key with it. This allows subscriptions to be made to the topic.
      /// </summary>
      void Create(TKey key);
      
      /// <summary>
      /// Deletes topic with given key and causes all events for this topic be invoked immediately and clears all enqueued events. 
      /// </summary>
      void Delete(TKey key);
      
      /// <summary>
      /// Publishes event to the queue.
      /// </summary>
      void Publish(TKey key, in TArgs args);
   }

   /// <summary>
   /// Event queue that supports generic event dispatch for multiple event sources and allows single topic to contain
   /// multiple events at any time.
   /// </summary>
   public class SharedEventQueue<TKey, TArgs> : IEventQueue<TKey, TArgs>
   {
      #region Static fields
      private static readonly CollectionPool<List<EventCallbackDelegate<TArgs>>> CallbackListPool = new CollectionPool<List<EventCallbackDelegate<TArgs>>>(
         new Pool<List<EventCallbackDelegate<TArgs>>>(new LinearStorageObject<List<EventCallbackDelegate<TArgs>>>(
                                                         new LinearGrowthArray<List<EventCallbackDelegate<TArgs>>>()))
         );
      
      private static readonly CollectionPool<List<TArgs>> EventListPool = new CollectionPool<List<TArgs>>(
         new Pool<List<TArgs>>(new LinearStorageObject<List<TArgs>>(
                                  new LinearGrowthArray<List<TArgs>>()))
         );
      #endregion
      
      #region Fields
      private readonly Dictionary<TKey, List<TArgs>> published;
      private readonly Dictionary<TKey, List<EventCallbackDelegate<TArgs>>> callbacks;
      
      // Set of topic keys that have published events.
      private readonly HashSet<TKey> dirty;
      #endregion

      public SharedEventQueue()
      {
         published = new Dictionary<TKey, List<TArgs>>();
         callbacks = new Dictionary<TKey, List<EventCallbackDelegate<TArgs>>>();
         
         dirty = new HashSet<TKey>();
      }
      
      protected void AssertTopicExists(TKey key)
      {
         if (!callbacks.ContainsKey(key))
            throw new InvalidOperationException($"topic with key {key} does not exist");
      }
      
      protected void AssertTopicDoesNotExist(TKey key)
      {
         if (callbacks.ContainsKey(key))
            throw new InvalidOperationException($"topic with key {key} already exists");
      }

      public bool Exists(TKey key)
         => callbacks.ContainsKey(key);

      public void Create(TKey key)
      {
         AssertTopicDoesNotExist(key);
         
         callbacks.Add(key, CallbackListPool.Take());
         published.Add(key, EventListPool.Take());
      }

      public void Subscribe(TKey key, EventCallbackDelegate<TArgs> callback)
      {
         AssertTopicExists(key);

         callbacks[key].Add(callback);
      }
         
      public void Unsubscribe(TKey key, EventCallbackDelegate<TArgs> callback)
      {
         AssertTopicExists(key);

         callbacks[key].Remove(callback);
      }
      
      public virtual void Publish(TKey key, in TArgs args)
      {
         AssertTopicExists(key);

         published[key].Add(args);
         
         dirty.Add(key);
      }
      
      public void Delete(TKey key)
      {
         AssertTopicExists(key);

         var args     = published[key];
         var handlers = callbacks[key];
         
         published.Remove(key);
         callbacks.Remove(key);

         foreach (var arg in args)
            foreach (var callback in handlers)
               callback(arg);
         
         EventListPool.Return(args);
         CallbackListPool.Return(handlers);
      }

      public virtual void Dispatch()
      {
         foreach (var key in dirty.Where(key => published.ContainsKey(key)))
         {
            foreach (var args in published[key])
            {
               foreach (var callback in callbacks[key])
                  callback(args);
            }
               
            published[key].Clear();
         }

         dirty.Clear();
      }
   }

   /// <summary>
   /// Event queue that supports generic event dispatch for multiple event sources
   /// and allows single topic to contain one event at time.
   /// </summary>
   public sealed class UniqueEventQueue<TKey, TArgs> : SharedEventQueue<TKey, TArgs> 
   {
      #region Fields
      private readonly HashSet<TKey> lookup;
      #endregion
      
      public UniqueEventQueue()
      {
         lookup = new HashSet<TKey>();
      }
      
      public override void Publish(TKey key, in TArgs args)
      {
         AssertTopicExists(key);

         if (!lookup.Add(key))
            return;
            
         base.Publish(key, args);
      }

      public override void Dispatch()
      {
         base.Dispatch();
         
         lookup.Clear();
      }
   }
}