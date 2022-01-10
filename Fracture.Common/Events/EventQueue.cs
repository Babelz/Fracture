using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
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
   
   public interface IEventHandler<TTopic, TArgs>
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
   
   public interface IEventQueue
   {
      /// <summary>
      /// Clears the queue and removes all unconsumed letters from it. 
      /// </summary>
      void Clear();
   }
   
   public interface ISharedEventPublisher<TTopic, TArgs> : IEventBacklog<TTopic>
   {
      /// <summary>
      /// Publishes given letter to given topic.
      /// </summary>
      void Publish(in TTopic topic, in TArgs args);
   }
   
   public delegate TArgs UniqueEventLetterAggregatorDelegate<TTopic, TArgs>(in Letter<TTopic, TArgs> current, in TArgs next);
   
   public interface IUniqueEventPublisher<TTopic, TArgs> : IEventBacklog<TTopic>
   {
      void Publish(in TTopic topic, in TArgs args, UniqueEventLetterAggregatorDelegate<TTopic, TArgs> aggregator = null);
   }

   public abstract class EventBacklog<TTopic, TArgs> : IEventBacklog<TTopic>, IEventHandler<TTopic, TArgs>, IEventQueue
   {
      #region Fields
      private readonly HashSet<TTopic> deletedTopics;
      private readonly HashSet<TTopic> activeTopics;
      
      private readonly HashSet<int> consumedLetters;
      
      private readonly LinearGrowthArray<Letter<TTopic, TArgs>> letters;
      
      private int publishedLettersCount;
      #endregion

      protected EventBacklog(int capacity)
      {
         activeTopics    = new HashSet<TTopic>();
         deletedTopics   = new HashSet<TTopic>();
         letters         = new LinearGrowthArray<Letter<TTopic, TArgs>>(capacity);
         consumedLetters = new HashSet<int>();
      }

      protected bool TopicExists(in TTopic topic)
         => activeTopics.Contains(topic);
      
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
   
   public sealed class SharedEventBacklog<TTopic, TArgs> : EventBacklog<TTopic, TArgs>, ISharedEventPublisher<TTopic, TArgs>
   {
      public SharedEventBacklog(int capacity)
         : base(capacity)
      {
      }
      
      public void Publish(in TTopic topic, in TArgs args)
      {
         if (!TopicExists(topic))
            throw new InvalidOperationException($"topic {topic} does not exist");
         
         EnqueueLetter(topic, args);
      }
   }
   
   public sealed class UniqueEventBacklog<TTopic, TArgs> : EventBacklog<TTopic, TArgs>, IUniqueEventPublisher<TTopic, TArgs>
   {
      #region Fields
      private readonly Dictionary<TTopic, int> topicLetterIndices;
      #endregion
      
      public UniqueEventBacklog(int capacity)
         : base(capacity) => topicLetterIndices = new Dictionary<TTopic, int>();
      
      public void Publish(in TTopic topic, in TArgs args, UniqueEventLetterAggregatorDelegate<TTopic, TArgs> aggregator = null)
      {
         if (!TopicExists(topic))
            throw new InvalidOperationException($"topic {topic} does not exist");
         
         if (topicLetterIndices.TryGetValue(topic, out var index))
            InsertLetter(index, topic, (aggregator != null ? aggregator(LetterAtIndex(index), args) : args));
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