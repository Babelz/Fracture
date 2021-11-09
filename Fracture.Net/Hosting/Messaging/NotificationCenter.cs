namespace Fracture.Net.Hosting.Messaging
{
    public interface INotificationCenter
    {
        INotification Enqueue();
    }
    
    public delegate void NotificationHandlerDelegate(INotification notification);
    
    public interface INotificationHandler
    {
        void Handle(NotificationHandlerDelegate handler);
    }
    
    public sealed class NotificationCenter : INotificationCenter, INotificationHandler
    {
        #region Fields
        
        #endregion
        
        public NotificationCenter()
        {
        }
        
        public INotification Enqueue()
        {
            throw new System.NotImplementedException();
        }

        public void Handle(NotificationHandlerDelegate handler)
        {
            throw new System.NotImplementedException();
        }
    }
}