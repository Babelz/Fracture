using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Runtime.CompilerServices;
using System.ServiceModel.Configuration;
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
    /// Structure defining notification that will be send using the notification pipeline. Notifications are generated in server free updates. Examples of
    /// a good use case for notification is world snapshot updates that are send to peers during world simulation updates.   
    /// </summary>
    public readonly struct Notification
    {
        #region Properties
        /// <summary>
        /// Gets the message in this notification.
        /// </summary>
        public IMessage Message
        {
            get;
        }

        /// <summary>
        /// Gets the command associated with this notification.
        /// </summary>
        public NotificationCommand Command
        {
            get;
        }

        /// <summary>
        /// Gets the optional peer list associated with this notification.
        /// </summary>
        public int[] Peers
        {
            get;
        }
        #endregion

        private Notification(IMessage message, NotificationCommand command, params int[] peers)
        {
            Message = message;
            Command = command;
            Peers   = peers;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AssertAtLeastOnePeerIsPresent(int[] peers)
        {
            if (peers?.Length == 0)
                throw new ArgumentException("expecting at least one peer to be present");
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Notification Send(int peerId, IMessage message)
            => new Notification(message, NotificationCommand.Send, peerId);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Notification Reset(int peerId, IMessage message = null)
            => new Notification(message, NotificationCommand.Reset, peerId);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Notification BroadcastNarrow(IMessage message, params int[] peers)
            => new Notification(message, NotificationCommand.BroadcastNarrow, peers);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Notification BroadcastWide(IMessage message)
            => new Notification(message, NotificationCommand.BroadcastWide);
    }
    
    /// <summary>
    /// Interface that provides functionality for notification based messaging. 
    /// </summary>
    public interface INotificationCenter
    {
        /// <summary>
        /// Enqueues given notification to the notification center to be processed.
        /// </summary>
        void Enqueue(in Notification notification);
    }
    
    /// <summary>
    /// Interface that provides functionality for processing notifications that have been enqueued. 
    /// </summary>
    public interface INotificationContainer
    {
        /// <summary>
        /// Gets next notification and returns boolean declaring whether the container still contains notifications. 
        /// </summary>
        bool Dequeue(out Notification notification);
    }
    
    /// <summary>
    /// Pipeline used for sending notifications to peers.
    /// </summary>
    public class NotificationPipeline : INotificationCenter, INotificationContainer
    {
        #region Fields
        private readonly LinearGrowthArray<Notification> notifications;
        
        private int begin;
        private int end;
        #endregion
        
        public NotificationPipeline(int initialNotificationsCapacity)
        {
            notifications = new LinearGrowthArray<Notification>(initialNotificationsCapacity);
        }
        
        public void Enqueue(in Notification notification)
        {
            if (end >= notifications.Length)
                notifications.Grow();
            
            notifications.Insert(end++, notification);
        }

        public bool Dequeue(out Notification notification)
        {
            var empty = begin > end;
            
            if (!empty)
                notification = notifications.AtIndex(begin++);
            else
            {
                notification = default;
                
                begin = 0;
                end   = 0;
            }
            
            return empty;
        }
    }
}