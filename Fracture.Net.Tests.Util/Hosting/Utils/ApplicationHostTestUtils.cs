using System;
using System.Runtime.CompilerServices;
using System.Security.Permissions;
using Fracture.Common.Di;
using Fracture.Net.Hosting;

namespace Fracture.Net.Tests.Util.Hosting.Utils
{
    /// <summary>
    /// Class that serves as application host for testing purposes. Do not use this in production as it leaks the internal IOC functionalities of the hosts.
    /// </summary>
    public sealed class TestApplicationHost : ApplicationHost
    {
        #region Properties
        /// <summary>
        /// Gets the service kernel of the application host. Access your services for testing purposes from this property.
        /// </summary>
        public Kernel ServiceKernel
        {
            get;
        }

        /// <summary>
        /// Gets the script kernel of the application host. Access your scripts for testing purposes from this property.
        /// </summary>
        public Kernel ScriptKernel
        {
            get;
        }
        #endregion

        public TestApplicationHost(Application application,
                                   ApplicationServiceHost services,
                                   ApplicationScriptingHost scripts,
                                   Kernel serviceKernel,
                                   Kernel scriptKernel)
            : base(application, services, scripts)
        {
            ServiceKernel = serviceKernel ?? throw new ArgumentNullException(nameof(serviceKernel));
            ScriptKernel  = scriptKernel ?? throw new ArgumentNullException(nameof(scriptKernel));
        }

        public void FrameAction(ulong frame, Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(frame));

            void Tick(object sender, EventArgs args)
            {
                if (Application.Clock.Ticks != frame)
                    return;

                action();

                Application.Tick -= Tick;
            }

            Application.Tick += Tick;
        }

        public void Start(ulong limit)
        {
            void Tick(object sender, EventArgs args)
            {
                if (Application.Clock.Ticks < limit)
                    return;
                
                Application.Shutdown();

                Application.Tick -= Tick;
            }

            Application.Tick += Tick;

            Start();
        }
    }

    /// <summary>
    /// Class that provides builder implementation for <see cref="TestApplicationHost"/>.
    /// </summary>
    public sealed class TestApplicationHostBuilder : BaseApplicationHostBuilder<TestApplicationHostBuilder, TestApplicationHost>
    {
        public TestApplicationHostBuilder(Application application)
            : base(application)
        {
        }

        public override TestApplicationHost Build()
            => new TestApplicationHost(Application,
                                       new ApplicationServiceHost(Services, Application),
                                       new ApplicationScriptingHost(Scripts, Application, Services.All<IApplicationService>()),
                                       Services,
                                       Scripts);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TestApplicationHostBuilder FromApplication(Application application)
            => new TestApplicationHostBuilder(application);
    }
}