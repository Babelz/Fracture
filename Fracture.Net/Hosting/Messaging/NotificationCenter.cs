namespace Fracture.Net.Hosting.Messaging
{
    public interface INotificationCenter
    {
        INotification Enqueue();
    }
    
    public delegate void NotificationHandlerDelegate(INotification notification);
    
    public interface INotificationDispatcher
    {
        void Dispatch(NotificationHandlerDelegate handler);
    }
    
    public sealed class NotificationCenter : INotificationCenter, INotificationDispatcher
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

        public void Dispatch(NotificationHandlerDelegate handler)
        {
            throw new System.NotImplementedException();
        }
    }
}