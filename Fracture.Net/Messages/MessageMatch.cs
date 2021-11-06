using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Fracture.Net.Messages
{
    /// <summary>
    /// Delegate for creating message match delegates. Message matchers are used in routing the messages in type based routing setup.
    /// </summary>
    public delegate bool MessageMatchDelegate(IMessage message);
    
    /// <summary>
    /// Static utility class containing message type matching utilises. 
    /// </summary>
    public static class MessageMatch
    {
        /// <summary>
        /// Matcher that accepts any message type and kind.
        /// </summary>
        public static MessageMatchDelegate Any() => (_) => true;
        
        /// <summary>
        /// Matcher that only accepts one specific message type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MessageMatchDelegate Exact<T>() where T : IMessage
            => (message) => message is T;
        
        /// <summary>
        /// Matcher that allows many message types.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MessageMatchDelegate Many(params Type[] messageTypes)
        {
            if (messageTypes.Any(t => typeof(IMessage).IsAssignableFrom(t)))
                throw new ArgumentException($"every type must be assignable to {nameof(IMessage)}", nameof(messageTypes));
            
            var messageTypesHash = new HashSet<Type>(messageTypes);
            
            return (message) => messageTypesHash.Contains(message.GetType());
        }
        
        /// <summary>
        /// Matcher that accepts all message types expect one. 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MessageMatchDelegate Exclude<T>() where T : IMessage
        {
            var exact = Exact<T>();
            
            return (message) => !exact(message);
        }
        
        /// <summary>
        /// Match that accepts all message types expect the ones specified.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MessageMatchDelegate ExcludeMany(params Type[] messageTypes)
        {
            var many = Many(messageTypes);
            
            return (message) => many(message);
        }
        
        /// <summary>
        /// Generic matcher that allows customs matching of message routes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MessageMatchDelegate Match(Func<IMessage, bool> predicate) 
            => (message) => predicate(message);
    }
}