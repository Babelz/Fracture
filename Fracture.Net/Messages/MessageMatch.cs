using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Fracture.Net.Messages
{
    /// <summary>
    /// Delegate for creating message match delegates. Message matchers are used in routing the messages in type based routing setup.
    /// </summary>
    public delegate bool MessageMatchDelegate(in IMessage message);

    /// <summary>
    /// Static utility class containing message type matching utilities. 
    /// </summary>
    public static class MessageMatch
    {
        /// <summary>
        /// Matcher that accepts any message type and kind.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MessageMatchDelegate Any()
            => delegate
            {
                return true;
            };

        /// <summary>
        /// Matcher that only accepts one specific message type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MessageMatchDelegate Exact<T>() where T : IMessage
            => (in IMessage message) => message is T;

        /// <summary>
        /// Matcher that only accepts one specific message type with specific message details.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MessageMatchDelegate Exact<T>(Predicate<T> predicate) where T : IMessage
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            return (in IMessage message) => message is T value && predicate(value);
        }

        /// <summary>
        /// Matcher that allows many message types.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MessageMatchDelegate Many(params Type[] messageTypes)
        {
            if (messageTypes.Any(t => typeof(IMessage).IsAssignableFrom(t)))
                throw new ArgumentException($"every type must be assignable to {nameof(IMessage)}", nameof(messageTypes));

            var messageTypesHash = new HashSet<Type>(messageTypes);

            return (in IMessage message) => messageTypesHash.Contains(message.GetType());
        }

        /// <summary>
        /// Matcher that accepts all message types expect one. 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MessageMatchDelegate Exclude<T>() where T : IMessage
        {
            var exact = Exact<T>();

            return (in IMessage message) => !exact(message);
        }

        /// <summary>
        /// Match that accepts all message types expect the ones specified.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MessageMatchDelegate ExcludeMany(params Type[] messageTypes)
        {
            var many = Many(messageTypes);

            return (in IMessage message) => many(message);
        }
    }
}