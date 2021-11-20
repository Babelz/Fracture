using System.Collections.Generic;
using Fracture.Common.Collections;
using Fracture.Common.Memory.Pools;
using Fracture.Common.Memory.Storages;

namespace Fracture.Net.Hosting.Messaging
{
    public delegate void NotificationDecoratorDelegate(INotification notification);
    
    /// <summary>
    /// Interface for implementing notification queues.
    /// </summary>
    public interface INotificationQueue
    {
        /// <summary>
        /// Enqueues notification to the queue and returns it to the caller.
        /// </summary>
        INotification Enqueue(NotificationDecoratorDelegate decoratorDelegate = null);
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
        /// Handles all queued notifications and invokes given handler callback for them. The queue will be cleared after the handle method returns.
        /// </summary>
        void Handle(NotificationHandlerDelegate handler);
    }
    
    /// <summary>
    /// Default implementation of <see cref="INotificationCenter"/>.  
    /// </summary>
    public sealed class NotificationCenter : INotificationCenter
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
            
        public INotification Enqueue(NotificationDecoratorDelegate decorator = null)
        {
            var notification = pool.Take();
            
            decorator?.Invoke(notification);
            
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