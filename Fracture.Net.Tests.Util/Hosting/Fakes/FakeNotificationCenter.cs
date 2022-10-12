using System;
using System.Collections.Generic;
using Fracture.Net.Hosting.Messaging;

namespace Fracture.Net.Tests.Util.Hosting.Fakes
{
    public sealed class NotificationEventArgs : EventArgs
    {
        #region Properties
        public INotification Notification
        {
            get;
        }
        #endregion

        public NotificationEventArgs(INotification notification)
        {
            Notification = notification ?? throw new ArgumentNullException(nameof(notification));
        }
    }

    public sealed class FakeNotificationCenter : INotificationCenter
    {
        #region Fields
        private readonly Queue<Notification> notifications;
        #endregion

        #region Events
        public event EventHandler<NotificationEventArgs> Enqueued;

        public event EventHandler<NotificationEventArgs> Handled;
        #endregion

        public FakeNotificationCenter()
        {
            notifications = new Queue<Notification>();
        }

        public void Enqueue(NotificationDecoratorDelegate decorator)
        {
            var notification = Notification.Take();

            decorator(notification);

            Enqueued?.Invoke(this, new NotificationEventArgs(notification));
            
            notifications.Enqueue(notification);
        }

        public INotification Enqueue()
        {
            var notification = Notification.Take();

            Enqueued?.Invoke(this, new NotificationEventArgs(notification));

            return notification;
        }

        public void Handle(NotificationHandlerDelegate handler)
        {
            while (notifications.Count != 0)
            {
                var notification = notifications.Dequeue();

                handler(notification);

                Handled?.Invoke(this, new NotificationEventArgs(notification));
            }
        }
    }
}