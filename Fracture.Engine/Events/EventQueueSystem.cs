using System;
using System.Collections.Generic;
using System.Linq;
using Fracture.Common.Events;
using Fracture.Engine.Core;

namespace Fracture.Engine.Events
{
    /// <summary>
    /// Interface for implementing event queue systems. These systems provide interface for publishing and handling events.
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
        #region Private lazy event handler class
        private sealed class LazyEventHandler<TTopic, TArgs> : IEventHandler<TTopic, TArgs>
        {
            #region Properties
            public IEventHandler<TTopic, TArgs> Value
            {
                get;
                set;
            }
            #endregion

            public LazyEventHandler()
            {
            }

            public void Handle(EventHandlerDelegate<TTopic, TArgs> handler)
                => Value?.Handle(handler);
        }
        #endregion

        #region Fields
        private readonly List<IEventQueue> queues;

        private readonly Dictionary<Type, object> handlers;
        #endregion

        public EventQueueSystem()
        {
            queues   = new List<IEventQueue>();
            handlers = new Dictionary<Type, object>();
        }

        private void AssertEventDoesNotExist<TTopic, TArgs>()
        {
            if (queues.Any(q => q.GetType().GetGenericArguments().SequenceEqual(new [] { typeof(TTopic), typeof(TArgs) })))
                throw new InvalidOperationException($"event with given topic and argument types already exists")
                {
                    Data =
                    {
                        { nameof(TTopic), typeof(TTopic) },
                        { nameof(TArgs), typeof(TArgs) }
                    }
                };
        }

        private void UpdateHandlers<TTopic, TArgs>(IEventHandler<TTopic, TArgs> backlog)
        {
            if (handlers.TryGetValue(typeof(IEventHandler<TTopic, TArgs>), out var handler))
                (handler as LazyEventHandler<TTopic, TArgs>)!.Value = backlog;
            else
                handlers.Add(typeof(IEventHandler<TTopic, TArgs>), new LazyEventHandler<TTopic, TArgs> { Value = backlog });
        }

        public ISharedEvent<TTopic, TArgs> CreateShared<TTopic, TArgs>(int capacity, LetterRetentionPolicy retentionPolicy)
        {
            AssertEventDoesNotExist<TTopic, TArgs>();

            var backlog = new SharedEvent<TTopic, TArgs>(capacity, retentionPolicy);

            queues.Add(backlog);

            UpdateHandlers(backlog);

            return backlog;
        }

        public IUniqueEvent<TTopic, TArgs> CreateUnique<TTopic, TArgs>(int capacity, LetterRetentionPolicy retentionPolicy)
        {
            AssertEventDoesNotExist<TTopic, TArgs>();

            var backlog = new UniqueEvent<TTopic, TArgs>(capacity, retentionPolicy);

            queues.Add(backlog);

            UpdateHandlers(backlog);

            return backlog;
        }

        public IEventHandler<TTopic, TArgs> GetEventHandler<TTopic, TArgs>()
        {
            var handler = (IEventHandler<TTopic, TArgs>)queues.FirstOrDefault(q => q is IEventHandler<TTopic, TArgs>);

            if (handler != null)
                return handler;

            handler = new LazyEventHandler<TTopic, TArgs>();

            handlers.Add(typeof(IEventHandler<TTopic, TArgs>), handler);

            return handler;
        }

        public override void Update(IGameEngineTime time)
        {
            foreach (var queue in queues)
                queue.Clear();
        }
    }
}