using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.CompilerServices;
using System.ServiceModel.Configuration;
using Castle.DynamicProxy;
using Fracture.Common.Collections;
using Fracture.Common.Memory;
using Fracture.Net.Messages;

namespace Fracture.Net.Hosting.Messaging
{
    /// <summary>
    /// Enumeration defining all possible notification command codes.
    /// </summary>
    public enum NotificationCommand : byte
    {
        /// <summary>
        /// Notification will do nothing.
        /// </summary>
        Unset = 0,
        
        /// <summary>
        /// Notification contains single send message, this is send to one peer only. 
        /// </summary>
        Send,
        
        /// <summary>
        /// Notification message will be broadcast to all peers listed in the message. 
        /// </summary>
        BroadcastNarrow,
        
        /// <summary>
        /// Notification message will be broadcast to all active peers.
        /// </summary>
        BroadcastWide,
        
        /// <summary>
        /// Notification message is considered as last message and the peer will be disconnected after the message has been send.
        /// </summary>
        Reset
    }
    
    /// <summary>
    /// Interface defining notification that will be handled using the notification pipeline. Notifications are generated in server free updates. Examples of
    /// a good use case for notification is world snapshot updates that are send to peers during world simulation updates.   
    /// </summary>
    public interface INotification
    {
        #region Properties
        /// <summary>
        /// Gets the message in this notification.
        /// </summary>
        IMessage Message
        {
            get;
        }

        /// <summary>
        /// Gets the command associated with this notification.
        /// </summary>
        NotificationCommand Command
        {
            get;
        }

        /// <summary>
        /// Gets the optional peer list associated with this notification.
        /// </summary>
        uint[] Peers
        {
            get;
        }
        #endregion
        
        /// <summary>
        /// Enqueues message notification for handling. 
        /// </summary>
        /// <param name="peerId">id of the peer this message is send to</param>
        /// <param name="message">message that will be send</param>
        void Send(uint peerId, IMessage message);
        
        /// <summary>
        /// Enqueues reset notification for handling.
        /// </summary>
        /// <param name="peerId">peer that will be reset</param>
        /// <param name="message">optional last message send to the peer before resetting</param>
        void Reset(uint peerId, IMessage message = null);
        
        /// <summary>
        /// Enqueues broadcast message for handling. 
        /// </summary>
        /// <param name="message">message that will be broadcast</param>
        /// <param name="peers">peers that the message will be broadcast to</param>
        void BroadcastNarrow(IMessage message, params uint[] peers);
        
        /// <summary>
        /// Enqueues broadcast message for handling. This message will be send to all connected peers.
        /// </summary>
        /// <param name="message">message that will be send to all active peers</param>
        void BroadcastWide(IMessage message);
    }
    
    /// <summary>
    /// Default implementation of <see cref="INotification"/>. This implementation can be pooled and thus is mutable.
    /// </summary>
    public sealed class Notification : INotification, IClearable
    {
        #region Properties
        public IMessage Message
        {
            get;
            private set;
        }

        public NotificationCommand Command
        {
            get;
            private set;
        }

        public uint[] Peers
        {
            get;
            private set;
        }
        #endregion

        public Notification()
        {
        }

        private void AssertUnset()
        {
            if (Command != NotificationCommand.Unset)
                throw new InvalidOperationException("notification is not unset");
        }
        
        public void Send(uint peerId, IMessage message)
        {
            AssertUnset();
            
            Command = NotificationCommand.Send;
            Peers   = new[] { peerId };
            Message = message ?? throw new ArgumentException(nameof(message));
        }
        
        public void Reset(uint peerId, IMessage message = null)
        {   
            AssertUnset();

            Command = NotificationCommand.Reset;
            Peers   = new[] { peerId };
            Message = message;
        }
        
        public void BroadcastNarrow(IMessage message, params uint[] peers)
        {
            AssertUnset();

            if (peers?.Length == 0)
                throw new ArgumentException("expecting at least one peer to be present");

            Command = NotificationCommand.BroadcastNarrow;
            Peers   = peers;
            Message = message ?? throw new ArgumentException(nameof(message));
        }
        
        public void BroadcastWide(IMessage message)
        {
            AssertUnset();

            Command = NotificationCommand.BroadcastWide;
            Peers   = null;
            Message = message ?? throw new ArgumentException(nameof(message));
        }

        public void Clear()
        {
            Command = default;
            Peers   = default;
            Message = default;
        }
    }
    
    /// <summary>
    /// Structure containing middleware request context of single notification object. 
    /// </summary>
    public readonly struct NotificationMiddlewareContext : IMiddlewareRequestContext
    {
        #region Properties
        /// <summary>
        /// Gets the notification associated with the middleware request context.
        /// </summary>
        public INotification Notification
        {
            get;
        }
        #endregion

        public NotificationMiddlewareContext(INotification notification)
            => Notification = notification ?? throw new ArgumentNullException(nameof(notification));
    }
}