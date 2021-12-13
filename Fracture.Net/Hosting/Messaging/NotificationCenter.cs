using System;
using System.Collections.Generic;
using Fracture.Common.Memory.Pools;

namespace Fracture.Net.Hosting.Messaging
{
    /// <summary>
    /// Interface for implementing notification queues.
    /// </summary>
    public interface INotificationQueue
    {
        /// <summary>
        /// Enqueues notification to the queue and returns it to the caller.
        /// </summary>
        INotification Enqueue();
    }
    
    /// <summary>
    /// Delegate for creating handlers that are invoked for each notification.
    /// </summary>
    public delegate void NotificationHandlerDelegate(Notification notification);
    
    /// <summary>
    /// Interface that provides full notification center implementation by functioning as queue and handler.
    /// </summary>
    public interface INotificationCenter : INotificationQueue
    {
        /// <summary>
        /// Handles all queued notifications and invokes given handler callback for them. The queue will be cleared after the handle method returns. Calling
        /// this method also transfers the ownership of any notifications to the caller.
        /// </summary>
        void Handle(NotificationHandlerDelegate handler);
    }
    
    /// <summary>
    /// Default implementation of <see cref="INotificationCenter"/>.  
    /// </summary>
    public sealed class NotificationCenter : INotificationCenter
    {
        #region Fields
        private readonly Queue<Notification> notifications;
        #endregion
        
        public NotificationCenter()
        {
            notifications = new Queue<Notification>();
        }
            
        public INotification Enqueue()
        {
            var notification = Notification.Take();
            
            notifications.Enqueue(notification);
            
            return notification;
        }
        
        public void Handle(NotificationHandlerDelegate handler)
        {
            while (notifications.Count != 0)
                handler(notifications.Dequeue());
        }
    }
}