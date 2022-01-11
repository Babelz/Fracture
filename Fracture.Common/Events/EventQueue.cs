using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Fracture.Common.Collections;
using Fracture.Common.Memory.Pools;
using Fracture.Common.Memory.Storages;

namespace Fracture.Common.Events
{
   public readonly struct Letter<TTopic, TArgs>
   {
      #region Properties
      public TTopic Topic
      {
         get;
      }

      public TArgs Args
      {
         get;
      }
      #endregion

      public Letter(in TTopic topic, in TArgs args)
      {
         Topic = topic;
         Args  = args;
      }
   }
   
   public enum LetterHandlingResult : byte
   {
      Consume = 0,
      Retain
   }
   
   public delegate LetterHandlingResult EventHandlerDelegate<TTopic, TArgs>(in Letter<TTopic, TArgs> letter);
   
   public interface IEventQueue
   {
      /// <summary>
      /// Clears the queue and removes all unconsumed letters from it. 
      /// </summary>
      void Clear();
   }
   
   public interface IEventHandler<TTopic, TArgs> : IEventQueue
   {
      void Handle(EventHandlerDelegate<TTopic, TArgs> handler);
   }
   
   public interface IEventBacklog<TTopic>
   {
      /// <summary>
      /// Creates new topic allowing subscriptions to be made to the topic.
      /// </summary>
      void Create(in TTopic topic);
      
      /// <summary>
      /// Marks topic for deletion and deletes it after next tick. 
      /// </summary>
      void Delete(in TTopic topic);
   }

   public enum EventRetentionPolicy : byte
   {
      SilenceDeletedTopics = 0,
      PublishDeletedTopics
   }
   
   public interface ISharedEvent<TTopic, TArgs> : IEventBacklog<TTopic>
   {
      /// <summary>
      /// Publishes given letter to given topic.
      /// </summary>
      void Publish(in TTopic topic, in TArgs args);
   }
   
   public delegate TArgs UniqueEventLetterAggregatorDelegate<TTopic, TArgs>(in TArgs current, in TArgs next);
   
   public interface IUniqueEvent<TTopic, TArgs> : IEventBacklog<TTopic>
   {
      void Publish(in TTopic topic, in TArgs args, UniqueEventLetterAggregatorDelegate<TTopic, TArgs> aggregator = null);
   }

   public class EventBacklog<TTopic, TArgs> : IEventBacklog<TTopic>, IEventHandler<TTopic, TArgs>
   {
      #region Static properties
      public static EventBacklog<TTopic, TArgs> Empty
      {
         get;
      }
      #endregion
      
      #region Fields
      private readonly HashSet<TTopic> deletedTopics;
      private readonly HashSet<TTopic> activeTopics;
      
      private readonly HashSet<int> consumedLetters;
      
      private readonly LinearGrowthArray<Letter<TTopic, TArgs>> letters;
      
      private int publishedLettersCount;
      #endregion

      #region Properties
      protected EventRetentionPolicy RetentionPolicy
      {
         get;
      }
      #endregion
      
      static EventBacklog()
      {
         Empty = new EventBacklog<TTopic, TArgs>(1);
      }
      
      protected EventBacklog(int capacity, EventRetentionPolicy retentionPolicy = EventRetentionPolicy.PublishDeletedTopics)
      {
         RetentionPolicy = retentionPolicy;
         
         activeTopics    = new HashSet<TTopic>();
         deletedTopics   = new HashSet<TTopic>();
         letters         = new LinearGrowthArray<Letter<TTopic, TArgs>>(capacity);
         consumedLetters = new HashSet<int>();
      }

      protected bool TopicExists(in TTopic topic)
         => activeTopics.Contains(topic);
      
      protected bool TopicActive(in TTopic topic)
         => !deletedTopics.Contains(topic);
      
      protected int EnqueueLetter(in TTopic topic, in TArgs args)
      {
         if (publishedLettersCount >= letters.Length)
            letters.Grow();
         
         letters.Insert(publishedLettersCount, new Letter<TTopic, TArgs>(topic, args));
         
         return publishedLettersCount++;
      }
      
      protected void InsertLetter(int index, in TTopic topic, in TArgs args)
         => letters.Insert(index, new Letter<TTopic, TArgs>(topic, args));
      
      protected ref Letter<TTopic, TArgs> LetterAtIndex(int index)
         => ref letters.AtIndex(index);
      
      public void Handle(EventHandlerDelegate<TTopic, TArgs> handler)
      {
         if (handler == null)
            throw new ArgumentNullException(nameof(handler));
         
         for (var i = 0; i < publishedLettersCount; i++)
         {
            if (consumedLetters.Contains(i)) 
               continue;
            
            ref var letter = ref letters.AtIndex(i);
          
            if (RetentionPolicy == EventRetentionPolicy.SilenceDeletedTopics && deletedTopics.Contains(letter.Topic))
               continue;
            
            if (handler(letter) != LetterHandlingResult.Retain)
               consumedLetters.Add(i);
         }
      }

      public void Create(in TTopic topic)
      {
         if (!activeTopics.Add(topic))
            throw new InvalidOperationException($"topic {topic} already exists");
      }

      public void Delete(in TTopic topic)
      {
         if (!TopicExists(topic))
            throw new InvalidOperationException($"topic {topic} does not exist");
         
         if (!deletedTopics.Add(topic))
            throw new InvalidOperationException($"topic {topic} already marked for deletion");
      }

      public virtual void Clear()
      {
         // Remove all topics that were marked for deletion.
         foreach (var topic in deletedTopics)
            activeTopics.Remove(topic);
         
         deletedTopics.Clear();
         
         // Clear letter state.
         consumedLetters.Clear();
         
         publishedLettersCount = 0;
      }
   }
   
   public sealed class SharedEvent<TTopic, TArgs> : EventBacklog<TTopic, TArgs>, ISharedEvent<TTopic, TArgs>
   {
      public SharedEvent(int capacity, EventRetentionPolicy retentionPolicy = EventRetentionPolicy.PublishDeletedTopics)
         : base(capacity, retentionPolicy)
      {
      }
      
      public void Publish(in TTopic topic, in TArgs args)
      {
         if (!TopicExists(topic))
            throw new InvalidOperationException($"topic {topic} does not exist");
       
         if (!TopicActive(topic) && RetentionPolicy == EventRetentionPolicy.SilenceDeletedTopics)
            return;

         EnqueueLetter(topic, args);
      }
   }
   
   public sealed class UniqueEvent<TTopic, TArgs> : EventBacklog<TTopic, TArgs>, IUniqueEvent<TTopic, TArgs>
   {
      #region Fields
      private readonly Dictionary<TTopic, int> topicLetterIndices;
      #endregion
      
      public UniqueEvent(int capacity, EventRetentionPolicy retentionPolicy = EventRetentionPolicy.PublishDeletedTopics)
         : base(capacity, retentionPolicy) => topicLetterIndices = new Dictionary<TTopic, int>(capacity);
      
      public void Publish(in TTopic topic, in TArgs args, UniqueEventLetterAggregatorDelegate<TTopic, TArgs> aggregator = null)
      {
         if (!TopicExists(topic))
            throw new InvalidOperationException($"topic {topic} does not exist");
         
         if (!TopicActive(topic) && RetentionPolicy == EventRetentionPolicy.SilenceDeletedTopics)
            return;

         if (topicLetterIndices.TryGetValue(topic, out var index))
            InsertLetter(index, topic, (aggregator != null ? aggregator(LetterAtIndex(index).Args, args) : args));
         else
            topicLetterIndices[topic] = EnqueueLetter(topic, args);
      }

      public override void Clear()
      {
         base.Clear();
         
         topicLetterIndices.Clear();
      }
   }
}