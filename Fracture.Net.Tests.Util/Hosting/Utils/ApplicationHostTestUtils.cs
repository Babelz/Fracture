using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Permissions;
using Fracture.Common.Di;
using Fracture.Net.Hosting;

namespace Fracture.Net.Tests.Util.Hosting.Utils
{
    public sealed class FrameAction
    {
        #region Properties
        public ulong Frame
        {
            get;
        }

        public Action Action
        {
            get;
        }
        #endregion

        public FrameAction(ulong frame, Action action)
        {
            Frame  = frame;
            Action = action;
        }
    }
    
    public sealed class UnhandledFrameActionException : Exception
    {
        #region Properties
        public IReadOnlyCollection<FrameAction> Actions
        {
            get;
        }
        #endregion

        public UnhandledFrameActionException(IReadOnlyCollection<FrameAction> actions)
            : base($"total {actions.Count} actions were left unhandled after application shutdown")
        {
            Actions = actions;
        }
    }
    
    /// <summary>
    /// Class that serves as application host for testing purposes. Do not use this in production as it leaks the internal IOC functionalities of the hosts.
    /// </summary>
    public sealed class TestApplicationHost : ApplicationHost
    {
        #region Fields
        private readonly HashSet<FrameAction> actions;
        #endregion
        
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

            actions = new HashSet<FrameAction>();
        }

        public void FrameAction(ulong frame, Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var frameAction = new FrameAction(frame, action);
            
            actions.Add(frameAction);
            
            void Tick(object sender, EventArgs args)
            {
                if (Application.Clock.Ticks != frame)
                    return;

                actions.Remove(frameAction);
                
                action();

                Application.Tick -= Tick;
            }

            Application.Tick += Tick;
        }

        public void Start(ulong limit)
        {
            void ShuttingDown(object sender, EventArgs args)
            {
                if (actions.Any())
                    throw new UnhandledFrameActionException(actions);
            }
            
            void Tick(object sender, EventArgs args)
            {
                if (Application.Clock.Ticks < limit)
                    return;
                
                Application.Shutdown();
                
                Application.Tick -= Tick;
            }
            
            Application.ShuttingDown += ShuttingDown;
            Application.Tick         += Tick;

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