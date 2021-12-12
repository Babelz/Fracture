using System;
using System.Configuration;
using System.Runtime.CompilerServices;
using Fracture.Net.Hosting;
using Fracture.Net.Hosting.Servers;

namespace Fracture.Net.Tests.Hosting.Utils
{
    /// <summary>
    /// Static utility class that provides testing utilities for applications.
    /// </summary>
    public static class ApplicationTestUtils
    {
        /// <summary>
        /// Limits execution time of given application by given tick count.
        /// <param name="application">application that's execution will be limited to certain tick limit</param>
        /// <param name="limit">for how many ticks the application should run before shutdown is called</param>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Limit(Application application, ulong limit)
        {
            void Tick(object sender, EventArgs args)
            {
                if (application.Clock.Ticks < limit)
                    return;
                
                application.Shutdown();
                
                application.Tick -= Tick;
            }
            
            application.Tick += Tick;
        }
        
        /// <summary>
        /// Binds action to application loop that will be executed on specified frame.
        /// </summary>
        /// <param name="application">application that this action will be invoked from</param>
        /// <param name="frame">frame on which the action is performed</param>
        /// <param name="action">action performed during specified frame</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FrameAction(Application application, ulong frame, Action action)
        {
            if (application == null)
                throw new ArgumentNullException(nameof(application));

            if (action == null)
                throw new ArgumentNullException(nameof(frame));
            
            void Tick(object sender, EventArgs args)
            {
                if (application.Clock.Ticks != frame)
                    return;
                
                action();
                
                application.Tick -= Tick;
            }
            
            application.Tick += Tick;
        }
    }
}