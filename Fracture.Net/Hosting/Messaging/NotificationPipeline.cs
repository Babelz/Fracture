using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.CompilerServices;
using System.ServiceModel.Configuration;
using Castle.DynamicProxy;
using Fracture.Common.Collections;
using Fracture.Net.Messages;

namespace Fracture.Net.Hosting.Messaging
{
    /// <summary>
    /// Enumeration defining commands associated with notifications.
    /// </summary>
    public enum NotificationCommand : byte
    {
        /// <summary>
        /// Single send message, this is send to one peer only. 
        /// </summary>
        Send = 0,
        
        /// <summary>
        /// Message will be broadcast to all peers listed in the message. 
        /// </summary>
        BroadcastNarrow,
        
        /// <summary>
        /// Message will be broadcast to all active peers.
        /// </summary>
        BroadcastWide,
        
        /// <summary>
        /// Message is considered as last message and the peer will be disconnected after the message has been send.
        /// </summary>
        Reset
    }
    
    /// <summary>
    /// Structure defining notification that will be handled using the notification pipeline. Notifications are generated in server free updates. Examples of
    /// a good use case for notification is world snapshot updates that are send to peers during world simulation updates.   
    /// </summary>
    public struct Notification
    {
        #region Properties
        /// <summary>
        /// Gets the message in this notification.
        /// </summary>
        public IMessage Message
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the command associated with this notification.
        /// </summary>
        public NotificationCommand Command
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the optional peer list associated with this notification.
        /// </summary>
        public uint[] Peers
        {
            get;
            set;
        }
        #endregion
    }
    
    /// <summary>
    /// Interface that provides functionality for creating different type notifications. 
    /// </summary>
    public interface INotificationCenter
    {
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
    /// Delegate that is used to handle enqueued notifications.
    /// </summary>
    public delegate void NotificationHandlerDelegate(in Notification notification);
    
    /// <summary>
    /// Interface that provides functionality for processing notifications that have been enqueued. 
    /// </summary>
    public interface INotificationContainer
    {
        /// <summary>
        /// Handles all enqueued notifications using given delegate handler.
        /// </summary>
        void Handle(NotificationHandlerDelegate handler);
    }
    
    /// <summary>
    /// Pipeline for handling notifications.
    /// </summary>
    public class NotificationPipeline : INotificationCenter, INotificationContainer
    {
        #region Fields
        private readonly LinearGrowthArray<Notification> notifications;
        
        private int count;
        #endregion
        
        public NotificationPipeline(int initialNotificationsCapacity)
        {
            notifications = new LinearGrowthArray<Notification>(initialNotificationsCapacity);
        }
        
        private void EnsureCapacity()
        {
            if (count >= notifications.Length)
                notifications.Grow();
        }
        
        public void Send(uint peerId, IMessage message)
        {
            if (message == null)
                throw new ArgumentException(nameof(message));
            
            EnsureCapacity();
            
            ref var notification = ref notifications.AtIndex(count++);
            
            notification.Command = NotificationCommand.Send;
            notification.Peers   = new[] { peerId };
            notification.Message = message;
        }
        
        public void Reset(uint peerId, IMessage message = null)
        {
            EnsureCapacity();
            
            ref var notification = ref notifications.AtIndex(count++);
            
            notification.Command = NotificationCommand.Reset;
            notification.Peers   = new[] { peerId };
            notification.Message = message;
        }
        
        public void BroadcastNarrow(IMessage message, params uint[] peers)
        {
            if (peers?.Length == 0)
                throw new ArgumentException("expecting at least one peer to be present");

            if (message == null)
                throw new ArgumentException(nameof(message));

            EnsureCapacity();
            
            ref var notification = ref notifications.AtIndex(count++);
            
            notification.Command = NotificationCommand.BroadcastNarrow;
            notification.Peers   = peers;
            notification.Message = message;
        }
        
        public void BroadcastWide(IMessage message)
        {
            if (message == null)
                throw new ArgumentException(nameof(message));

            EnsureCapacity();
            
            ref var notification = ref notifications.AtIndex(count++);
            
            notification.Command = NotificationCommand.BroadcastWide;
            notification.Peers   = null;
            notification.Message = message;
        }

        public void Handle(NotificationHandlerDelegate handler)
        {
            if (handler == null)
                throw new ArgumentException(nameof(handler));
            
            for (var i = 0; i < count; i++)
                handler(notifications.AtIndex(i));
            
            count = 0;
        }
    }
}