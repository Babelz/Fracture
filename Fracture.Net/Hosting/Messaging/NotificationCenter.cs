using System.Collections.Generic;
using Fracture.Common.Collections;
using Fracture.Common.Memory.Pools;
using Fracture.Common.Memory.Storages;

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
    public delegate void NotificationHandlerDelegate(INotification notification);
    
    /// <summary>
    /// Interface for implementing notification handlers.
    /// </summary>
    public interface INotificationHandler
    {
        /// <summary>
        /// Handles all queued notifications and invokes given handler callback for them. The queue will be cleared after the handle method returns.
        /// </summary>
        void Handle(NotificationHandlerDelegate handler);
    }
    
    /// <summary>
    /// Class that provides full notification center implementation by functioning as queue and handler.  
    /// </summary>
    public sealed class NotificationCenter : INotificationQueue, INotificationHandler
    {
        #region Fields
        private readonly CleanPool<Notification> pool;
        
        private readonly Queue<Notification> notifications;
        #endregion
        
        public NotificationCenter(int initialNotificationsCapacity, int initialNotifications = 0)
        {
            pool = new CleanPool<Notification>(
                new Pool<Notification>(
                    new LinearStorageObject<Notification>(
                        new LinearGrowthArray<Notification>(initialNotificationsCapacity)), initialNotifications)
            );
            
            notifications = new Queue<Notification>();
        }
            
        public INotification Enqueue()
        {
            var notification = pool.Take();
            
            notifications.Enqueue(notification);
            
            return notification;
        }
        
        public void Handle(NotificationHandlerDelegate handler)
        {
            while (notifications.Count != 0)
            {
                var notification = notifications.Dequeue();
                
                handler(notification);
                
                pool.Return(notification);
            }
        }
    }
}