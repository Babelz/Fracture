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
    }
    
    public interface INotificationDecorator
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
    
    public sealed class Notification : INotification, INotificationDecorator, IClearable
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

        public void Send(uint peerId, IMessage message)
        {
            Command = NotificationCommand.Send;
            Peers   = new[] { peerId };
            Message = message ?? throw new ArgumentException(nameof(message));
        }
        
        public void Reset(uint peerId, IMessage message = null)
        {   
            Command = NotificationCommand.Reset;
            Peers   = new[] { peerId };
            Message = message;
        }
        
        public void BroadcastNarrow(IMessage message, params uint[] peers)
        {
            if (peers?.Length == 0)
                throw new ArgumentException("expecting at least one peer to be present");

            Command = NotificationCommand.BroadcastNarrow;
            Peers   = peers;
            Message = message ?? throw new ArgumentException(nameof(message));
        }
        
        public void BroadcastWide(IMessage message)
        {
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
}