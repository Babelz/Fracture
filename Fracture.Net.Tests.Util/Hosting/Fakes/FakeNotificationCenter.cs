using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using Fracture.Net.Hosting.Messaging;
using Fracture.Net.Tests.Util.Hosting.Utils;

namespace Fracture.Net.Tests.Util.Hosting.Fakes
{
    public sealed class UnexpectedNotificationException : Exception
    {
        #region Properties
        public INotification Notification
        {
            get;
        }
        #endregion

        public UnexpectedNotificationException(INotification notification)
            : base($"guarded against unexpectedly received notification {notification.GetType().Name}")
        {
            Notification = notification;
        }
    }
    
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

    public delegate void FakeNotificationInspectorDelegate(INotification notification);

    public delegate bool NotificationMatchDelegate(INotification notification);

    /// <summary>
    /// Enumeration that defines how notification inspector should behave.
    /// </summary>
    public enum FakeNotificationInspectorBehaviour : byte
    {
        /// <summary>
        /// The inspector is expecting this notification to be passed on specific frame.
        /// </summary>
        Expect,
        
        /// <summary>
        /// The inspector is guarding that notification of this type should not be passed on specific frame. 
        /// </summary>
        Guard
    }
    
    public sealed class FakeNotificationInspector
    {
        #region Properties
        public NotificationMatchDelegate Match
        {
            get;
        }

        public FakeNotificationInspectorDelegate Callback
        {
            get;
        }

        public FakeNotificationInspectorBehaviour Behaviour
        {
            get;
        }
        #endregion
        
        public FakeNotificationInspector(NotificationMatchDelegate match, FakeNotificationInspectorDelegate callback, FakeNotificationInspectorBehaviour behaviour)
        {
            Match     = match;
            Callback  = callback ?? throw new ArgumentNullException(nameof(callback));
            Behaviour = behaviour;
        }
    }

    public delegate void FakeNotificationFrameDecoratorDelegate(FakeNotificationFrame frame);
    
    public sealed class FakeNotificationFrame
    {
        #region Fields
        private readonly List<NotificationDecoratorDelegate> outgoingDecorators;
        #endregion

        #region Properties
        public string Name
        {
            get;
        }
        
        public FakeNotificationInspector Inspector
        {
            get;
            private set;
        }

        public IEnumerable<NotificationDecoratorDelegate> OutgoingDecorators
            => outgoingDecorators;

        public int ExpectedInspections
        {
            get;
            private set;
        }

        public ulong Frame
        {
            get;
        }
        #endregion

        private FakeNotificationFrame(string name, ulong frame)
        {
            Name               = !string.IsNullOrEmpty(name) ? name : throw new ArgumentNullException(nameof(name));
            Frame              = frame;
            outgoingDecorators = new List<NotificationDecoratorDelegate>();
        }

        public FakeNotificationFrame Expect(NotificationMatchDelegate match, FakeNotificationInspectorDelegate inspector)
        {
            Inspector = new FakeNotificationInspector(match, inspector, FakeNotificationInspectorBehaviour.Expect);
            
            return this;
        }

        public FakeNotificationFrame Expect(FakeNotificationInspectorDelegate callback)
            => Expect(null, callback);
        
        public FakeNotificationFrame Guard(NotificationMatchDelegate match, FakeNotificationInspectorDelegate inspector)
        {
            Inspector = new FakeNotificationInspector(match, inspector, FakeNotificationInspectorBehaviour.Guard);
            
            return this;
        }

        public FakeNotificationFrame Guard(FakeNotificationInspectorDelegate inspector)
            => Guard(null, inspector);

        public FakeNotificationFrame Expect(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            
            ExpectedInspections = count;

            return this;
        }

        public FakeNotificationFrame Outgoing(NotificationDecoratorDelegate inspector)
        {
            outgoingDecorators.Add(inspector ?? throw new ArgumentNullException(nameof(inspector)));

            return this;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FakeNotificationFrame[] Repeat(string name, FakeNotificationFrameDecoratorDelegate decorator, params ulong[] frames)
            => frames.Select(f =>
            {
                var frame = new FakeNotificationFrame(name, f);

                decorator(frame);

                return frame;
            }).ToArray();
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FakeNotificationFrame Create(string name, ulong frame)
            => new FakeNotificationFrame(name, frame);
    }

    public sealed class FakeNotificationCenter : INotificationCenter
    {
        #region Fields
        private readonly List<FakeNotificationFrame> frames;

        private readonly Queue<Notification> notifications;

        private ulong ticks;
        #endregion

        private FakeNotificationCenter(params FakeNotificationFrame[] frames)
        {
            this.frames = new List<FakeNotificationFrame>(frames ?? Array.Empty<FakeNotificationFrame>());

            notifications = new Queue<Notification>();
        }

        public void EnqueueFrame(FakeNotificationFrame frame)
            => frames.Add(frame);
        
        public void Enqueue(NotificationDecoratorDelegate decorator)
        {
            var notification = Notification.Take();

            notifications.Enqueue(notification);

            decorator(notification);
        }

        public INotification Enqueue()
        {
            var notification = Notification.Take();

            notifications.Enqueue(notification);
            
            return notification;
        }

        public void Handle(NotificationHandlerDelegate handler)
        {
            var activeFrames = frames.Where(f => f.Frame == ticks).ToArray();
            
            // Send out active frame notifications.
            foreach (var frame in activeFrames)
            {
                if (!frame.OutgoingDecorators.Any())
                    continue;

                foreach (var decorator in frame.OutgoingDecorators)
                {
                    var notification = Notification.Take();

                    decorator(notification);
                    
                    notifications.Enqueue(notification);
                }
            }

            // Keep track of handled inspectors.
            var unhandledExpectedInspectors = new HashSet<FakeNotificationFrame>(activeFrames.Where(
                f => f.Inspector != null && f.Inspector.Behaviour == FakeNotificationInspectorBehaviour.Expect)
            );
            
            // Keep track of frame inspector invocations.
            var inspectorInvocationsCount = unhandledExpectedInspectors.Where(f => f.ExpectedInspections > 0).ToDictionary(k => k, v => 0);
            
            while (notifications.Count != 0)
            {
                var notification = notifications.Dequeue();
                
                // Inspect.
                foreach (var frame in activeFrames)
                {
                    if (frame.Inspector == null)
                        continue;
                    
                    if (frame.Inspector.Match != null && !frame.Inspector.Match(notification))
                        continue;

                    frame.Inspector.Callback(notification);

                    // Update invocations count.
                    if (inspectorInvocationsCount.ContainsKey(frame))
                        inspectorInvocationsCount[frame]++;

                    // Mark frame as handled for now.
                    unhandledExpectedInspectors.Remove(frame);
                }
                
                // Allow normal application flow handling of notifications.
                handler(notification);
            }
            
            // Notify about leaking call counts.
            var inspectorsWithLeakingInvocationCount = inspectorInvocationsCount.Where(kvp => kvp.Key.ExpectedInspections != kvp.Value).ToArray();
            
            if (inspectorsWithLeakingInvocationCount.Any())
            {
                var leakingInspectorNamesList = string.Concat(inspectorsWithLeakingInvocationCount.Take(inspectorsWithLeakingInvocationCount.Length - 1)
                                                                                                  .Select(kvp => $"\"{kvp.Key.Name}\" - with calls " +
                                                                                                                 $"{kvp.Value}/{kvp.Key.ExpectedInspections}, "));

                var last = inspectorsWithLeakingInvocationCount.Last();
                
                leakingInspectorNamesList += $"{last.Key.Name} - with calls {last.Value}/{last.Key.ExpectedInspections}";
    
                // Too few or too many calls were made to the inspector.
                throw new InvalidOperationException($"during frame {ticks} total {inspectorsWithLeakingInvocationCount.Length} did not receive expected count of " +
                                                    $"notifications for inspection, see the frame configuration for the test that caused this error for more " +
                                                    $"details, list of leaking inspectors are: {leakingInspectorNamesList}");
            }
            
            // Notify about leaking inspectors.
            if (unhandledExpectedInspectors.Any())
            {
                var unhandledInspectorNamesList = string.Concat(unhandledExpectedInspectors.Take(unhandledExpectedInspectors.Count - 1).Select(f => $"\"{f.Name}\", "));

                unhandledInspectorNamesList += unhandledExpectedInspectors.Last().Name;
                
                throw new InvalidOperationException($"during frame {ticks} total {unhandledExpectedInspectors.Count()} frame inspectors did not receive expected " +
                                                    $"notification, see the frame configuration for the test that caused this error for more details, list of " +
                                                    $"unhandled frame names are: {unhandledInspectorNamesList}");
            }
                
            // Remove consumed frames.
            frames.RemoveAll(f => activeFrames.Contains(f));
            
            ticks++;
        }

        public void Shutdown()
        {
            if (frames.Any())
                throw new InvalidOperationException($"notification center was torn down but total {frames.Count()} frames are still queued");
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FakeNotificationCenter FromFrames(params FakeNotificationFrame[] frames)
            => new FakeNotificationCenter(frames);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FakeNotificationCenter Create(IEnumerable<FakeNotificationFrame> frames)
            => new FakeNotificationCenter(frames.ToArray());
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FakeNotificationCenter Create()
            => new FakeNotificationCenter();
    }
}