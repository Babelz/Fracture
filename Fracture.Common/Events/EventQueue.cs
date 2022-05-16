using System;
using System.Collections.Generic;
using Fracture.Common.Collections;

namespace Fracture.Common.Events
{
    /// <summary>
    /// Structure representing generic letter that contains the topic it was published to and the arguments.
    /// </summary>
    public readonly struct Letter<TTopic, TArgs>
    {
        #region Properties
        /// <summary>
        /// Gets the topic of this letter where it was published.
        /// </summary>
        public TTopic Topic
        {
            get;
        }

        /// <summary>
        /// Gets the arguments of this letter, this is the data associated with it.
        /// </summary>
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

    /// <summary>
    /// Enumeration defining letter handling results.
    /// </summary>
    public enum LetterHandlingResult : byte
    {
        /// <summary>
        /// Letter should be consumed and will not be put pack to the backlog.
        /// </summary>
        Consume = 0,

        /// <summary>
        /// Letter should be put back to the backlog.
        /// </summary>
        Retain
    }

    /// <summary>
    /// Delegate used for handling enqueued letters from the backlog.
    /// </summary>
    public delegate LetterHandlingResult EventHandlerDelegate<TTopic, TArgs>(in Letter<TTopic, TArgs> letter);

    /// <summary>
    /// Non-generic common interface for event queues.
    /// </summary>
    public interface IEventQueue
    {
        /// <summary>
        /// Clears the queue and removes all unconsumed letters from it. 
        /// </summary>
        void Clear();
    }

    /// <summary>
    /// Interface for creating backlogs. Backlogs contain set of topics that where letters can be pushed to. 
    /// </summary>
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

    /// <summary>
    /// Generic interface for implementing event handlers.
    /// </summary>
    public interface IEventHandler<TTopic, TArgs>
    {
        /// <summary>
        /// Handles all letters enqueued in the event.
        /// </summary>
        void Handle(EventHandlerDelegate<TTopic, TArgs> handler);
    }

    /// <summary>
    /// Enumeration defining letter retention policies.
    /// </summary>
    public enum LetterRetentionPolicy : byte
    {
        /// <summary>
        /// Letters from topics that are marked for deletion will not be handled. 
        /// </summary>
        SilenceDeletedTopics = 0,

        /// <summary>
        /// Letters from topics that are marked for deletion will be handled.
        /// </summary>
        PublishDeletedTopics
    }

    /// <summary>
    /// Interface for implementing shared publishers. Topics of shared events can contain multiple letters.
    /// </summary>
    public interface ISharedEvent<TTopic, TArgs> : IEventBacklog<TTopic>
    {
        /// <summary>
        /// Publishes given letter to given topic.
        /// </summary>
        void Publish(in TTopic topic, in TArgs args);
    }

    public delegate TArgs UniqueEventLetterAggregatorDelegate<TArgs>(in TArgs current, in TArgs next);

    /// <summary>
    /// Interface for implementing unique publishers. Topics of unique publishers can contain only one letter at time. 
    /// </summary>
    public interface IUniqueEvent<TTopic, TArgs> : IEventBacklog<TTopic>
    {
        /// <summary>
        /// Publishes given letter to the topic. If the topic already has letter associated with it the aggregator will be invoked.
        /// </summary>
        void Publish(in TTopic topic, in TArgs args, UniqueEventLetterAggregatorDelegate<TArgs> aggregator = null);
    }

    public class Event<TTopic, TArgs> : IEventQueue, IEventBacklog<TTopic>, IEventHandler<TTopic, TArgs>
    {
        #region Fields
        private readonly HashSet<TTopic> deletedTopics;
        private readonly HashSet<TTopic> activeTopics;

        private readonly HashSet<int> consumedLetters;

        private readonly LinearGrowthArray<Letter<TTopic, TArgs>> letters;

        private int publishedLettersCount;
        #endregion

        #region Properties
        protected LetterRetentionPolicy RetentionPolicy
        {
            get;
        }
        #endregion

        protected Event(int capacity, LetterRetentionPolicy retentionPolicy = LetterRetentionPolicy.PublishDeletedTopics)
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

        protected void UpdateLetter(int index, in TTopic topic, in TArgs args)
        {
            if (index >= publishedLettersCount)
                throw new ArgumentOutOfRangeException($"{nameof(index)} is not in the range of {nameof(publishedLettersCount)}")
                {
                    Data =
                    {
                        { nameof(index), index },
                        { nameof(publishedLettersCount), publishedLettersCount }
                    }
                };

            letters.Insert(index, new Letter<TTopic, TArgs>(topic, args));
        }

        protected ref Letter<TTopic, TArgs> LetterAtIndex(int index)
        {
            if (index >= publishedLettersCount)
                throw new ArgumentOutOfRangeException($"{nameof(index)} is not in the range of {nameof(publishedLettersCount)}")
                {
                    Data =
                    {
                        { nameof(index), index },
                        { nameof(publishedLettersCount), publishedLettersCount }
                    }
                };

            return ref letters.AtIndex(index);
        }

        public void Handle(EventHandlerDelegate<TTopic, TArgs> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            for (var i = 0; i < publishedLettersCount; i++)
            {
                if (consumedLetters.Contains(i))
                    continue;

                ref var letter = ref letters.AtIndex(i);

                if (RetentionPolicy == LetterRetentionPolicy.SilenceDeletedTopics && deletedTopics.Contains(letter.Topic))
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

    public sealed class SharedEvent<TTopic, TArgs> : Event<TTopic, TArgs>, ISharedEvent<TTopic, TArgs>
    {
        public SharedEvent(int capacity, LetterRetentionPolicy retentionPolicy = LetterRetentionPolicy.PublishDeletedTopics)
            : base(capacity, retentionPolicy)
        {
        }

        public void Publish(in TTopic topic, in TArgs args)
        {
            if (!TopicExists(topic))
                throw new InvalidOperationException($"topic {topic} does not exist");

            if (!TopicActive(topic) && RetentionPolicy == LetterRetentionPolicy.SilenceDeletedTopics)
                return;

            EnqueueLetter(topic, args);
        }
    }

    public sealed class UniqueEvent<TTopic, TArgs> : Event<TTopic, TArgs>, IUniqueEvent<TTopic, TArgs>
    {
        #region Fields
        private readonly Dictionary<TTopic, int> topicLetterIndices;
        #endregion

        public UniqueEvent(int capacity, LetterRetentionPolicy retentionPolicy = LetterRetentionPolicy.PublishDeletedTopics)
            : base(capacity, retentionPolicy)
            => topicLetterIndices = new Dictionary<TTopic, int>(capacity);

        public void Publish(in TTopic topic, in TArgs args, UniqueEventLetterAggregatorDelegate<TArgs> aggregator = null)
        {
            if (!TopicExists(topic))
                throw new InvalidOperationException($"topic {topic} does not exist");

            if (!TopicActive(topic) && RetentionPolicy == LetterRetentionPolicy.SilenceDeletedTopics)
                return;

            if (topicLetterIndices.TryGetValue(topic, out var index))
                UpdateLetter(index, topic, (aggregator != null ? aggregator(LetterAtIndex(index).Args, args) : args));
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