using System;
using System.Runtime.CompilerServices;
using Fracture.Net.Hosting;

namespace Fracture.Net.Tests.Util.Hosting.Utils
{
    /// <summary>
    /// Delegate that is used for decorating the <see cref="TestApplicationHostBuilder"/> before building the host.
    /// </summary>
    public delegate void TestApplicationHostBuilderDecorator(TestApplicationHostBuilder builder);
    
    /// <summary>
    /// Static utility class that provides testing utilities for applications.
    /// </summary>
    public static class ApplicationTestUtils
    {
        /// <summary>
        /// Creates application and the application host for testing purposes and returns single service from the build application host for testing it.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T CreateTestService<T>(ApplicationBuilder applicationBuilder, 
                                             TestApplicationHostBuilderDecorator decorator = null) where T : class, IApplicationService
        {
            var hostBuilder = TestApplicationHostBuilder.FromApplication(applicationBuilder.Build());
            
            decorator?.Invoke(hostBuilder);
            
            return hostBuilder.Service<T>().Build().ServiceKernel.First<T>();
        }

        /// <summary>
        /// Creates application and the application host for testing purposes and returns single script from the build application host for testing it.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T CreateTestScript<T>(ApplicationBuilder applicationBuilder,
                                            TestApplicationHostBuilderDecorator decorator = null) where T : class, IApplicationScript
        {
            var hostBuilder = TestApplicationHostBuilder.FromApplication(applicationBuilder.Build());
            
            decorator?.Invoke(hostBuilder);
            
            return hostBuilder.Script<T>().Build().ServiceKernel.First<T>();
        }

        /// <summary>
        /// Limits execution time of given application by given tick count.
        /// <param name="application">application that's execution will be limited to certain tick limit</param>
        /// <param name="limit">for how many ticks the application should run before shutdown is called</param>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LimitFrames(Application application, ulong limit)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RunForTicks(IActiveApplicationScript script, ulong ticks)
        {
            for (var i = 0u; i < ticks; i++)
                script.Tick();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RunForTicks(IActiveApplicationService service, ulong ticks)
        {
            for (var i = 0u; i < ticks; i++)
                service.Tick();
        }
    }
}