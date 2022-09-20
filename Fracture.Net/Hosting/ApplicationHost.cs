using System;
using System.Collections.Generic;
using System.Linq;
using Fracture.Common.Di;
using Fracture.Common.Di.Attributes;
using Fracture.Common.Di.Binding;
using Fracture.Common.Events;
using Fracture.Net.Hosting.Messaging;
using Fracture.Net.Hosting.Servers;
using Serilog;

namespace Fracture.Net.Hosting
{
    /// <summary>
    /// Interface that provides common application host schematics between different host types. Application hosts extend the application by providing service
    /// and script hosting for isolating different parts of the application from each other.
    /// </summary>
    public interface IApplicationHost
    {
        #region Properties
        /// <summary>
        /// Gets the application clock containing current application time.
        /// </summary>
        IApplicationClock Clock
        {
            get;
        }
        #endregion

        /// <summary>
        /// Signals the application to start shutting down.
        /// </summary>
        void Shutdown();
    }

    /// <summary>
    /// Interface for application hosts that provides interface for services to interact with the application.
    /// </summary>
    public interface IApplicationServiceHost : IApplicationHost
    {
        #region Events
        /// <summary>
        /// Event invoked when the application is about to start.
        /// </summary>
        event EventHandler Starting;

        /// <summary>
        /// Event invoked when the application is about to shut down.
        /// </summary>
        event EventHandler ShuttingDown;
        #endregion
    }

    /// <summary>
    /// Interface for application hosts that provide interface for scripts to interact with the application.
    /// </summary>
    public interface IApplicationScriptingHost : IApplicationHost
    {
        #region Events
        /// <summary>
        /// Event invoked when peer has joined.
        /// </summary>
        event StructEventHandler<PeerJoinEventArgs> Join;

        /// <summary>
        /// Event invoked when peer has reset.
        /// </summary>
        event StructEventHandler<PeerResetEventArgs> Reset;

        /// <summary>
        /// Event invoked when peer makes bad request that can't be deserialized properly by the server.
        /// </summary>
        event StructEventHandler<PeerMessageEventArgs> BadRequest;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the application request context for working with the request pipeline.
        /// </summary>
        ApplicationRequestContext Requests
        {
            get;
        }

        /// <summary>
        /// Gets the application notification context for working with notification pipeline.
        /// </summary>
        ApplicationNotificationContext Notifications
        {
            get;
        }

        /// <summary>
        /// Gets the application response middleware consumer.
        /// </summary>
        IMiddlewareConsumer<RequestResponseMiddlewareContext> Responses
        {
            get;
        }

        /// <summary>
        /// Gets all peers currently connected to the application.
        /// </summary>
        IEnumerable<int> PeerIds
        {
            get;
        }
        #endregion

        void Load(Type type, params IBindingValue[] args);
    }

    /// <summary>
    /// Default implementation of <see cref="IApplicationServiceHost"/>.
    /// </summary>
    public sealed class ApplicationServiceHost : IApplicationServiceHost
    {
        #region Fields
        private readonly Application application;

        private readonly IDependencyLocator services;
        #endregion

        #region Events
        public event EventHandler Starting;
        public event EventHandler ShuttingDown;
        #endregion

        #region Properties
        public IApplicationClock Clock => application.Clock;
        #endregion

        [BindingConstructor]
        public ApplicationServiceHost(Kernel services, Application application)
        {
            this.application = application ?? throw new ArgumentNullException(nameof(application));
            this.services    = services ?? throw new ArgumentNullException(nameof(services));

            services.Bind(this);

            application.ShuttingDown += delegate
            {
                ShuttingDown?.Invoke(this, EventArgs.Empty);
            };

            application.Starting += delegate
            {
                Starting?.Invoke(this, EventArgs.Empty);
            };

            foreach (var service in services.All<IApplicationService>())
                Log.Information($"loaded service {service.GetType().FullName} at startup...");

            services.Verify();
        }

        public void Tick()
        {
            foreach (var service in services.All<IActiveApplicationService>())
            {
                try
                {
                    service.Tick();
                }
                catch (Exception e)
                {
                    Log.Warning(e, "unhandled error occurred while updating service", service);
                }
            }
        }

        public void Shutdown()
            => application.Shutdown();
    }

    /// <summary>
    /// Default implementation of <see cref="IApplicationScriptingHost"/>.
    /// </summary>
    public sealed class ApplicationScriptingHost : IApplicationScriptingHost
    {
        #region Fields
        private readonly Application application;

        private readonly Kernel scripts;
        #endregion

        #region Events
        public event StructEventHandler<PeerJoinEventArgs> Join;
        public event StructEventHandler<PeerResetEventArgs> Reset;
        public event StructEventHandler<PeerMessageEventArgs> BadRequest;
        #endregion

        #region Properties
        public ApplicationRequestContext Requests => application.Requests;

        public ApplicationNotificationContext Notifications => application.Notifications;

        public IMiddlewareConsumer<RequestResponseMiddlewareContext> Responses => application.Responses;

        public IApplicationClock Clock => application.Clock;

        public IEnumerable<int> PeerIds => application.PeerIds;
        #endregion

        public ApplicationScriptingHost(Kernel scripts, Application application, IEnumerable<IApplicationService> services)
        {
            this.application = application ?? throw new ArgumentNullException(nameof(application));
            this.scripts     = scripts ?? throw new ArgumentNullException(nameof(scripts));

            // Bind services to scripts.
            foreach (var service in services)
                scripts.Bind(service);

            // Bind host to kernel.
            scripts.Bind(this);

            // Unload all scripts when the application exists.
            application.ShuttingDown += delegate
            {
                Log.Information("application shutdown signaled, unloading all scripts...");

                foreach (var script in scripts.All<IApplicationScript>().ToList())
                {
                    try
                    {
                        Log.Information($"unloading script ${script.GetType().FullName}...");

                        script.Unload();
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "unhandled error occurred while unloading script");
                    }
                }
            };

            application.Join       += (object sender, in PeerJoinEventArgs e) => Join?.Invoke(this, e);
            application.Reset      += (object sender, in PeerResetEventArgs e) => Reset?.Invoke(this, e);
            application.BadRequest += (object sender, in PeerMessageEventArgs e) => BadRequest?.Invoke(this, e);

            foreach (var script in scripts.All<IApplicationScript>())
                Log.Information($"loaded script {script.GetType().FullName} at startup...");

            scripts.Verify();
        }

        public void Load(Type type, params IBindingValue [] args)
        {
            if (!typeof(IApplicationScript).IsAssignableFrom(type))
                throw new ArgumentException($"{type.Name} is not a script type", nameof(type));

            if (type.IsAbstract || type.IsInterface)
                throw new ArgumentException($"{type.Name} is abstract", nameof(type));

            if (type.IsValueType)
                throw new ArgumentException($"{type.Name} is a value type", nameof(type));

            var script = (IApplicationScript)scripts.Activate(type, args);

            switch (script)
            {
                case IApplicationCommandScript ics:
                    Log.Information($"loaded command script: {type.Name}...");

                    break;
                case IActiveApplicationScript ias:
                    Log.Information($"loaded active script: {type.Name}...");

                    break;
                default:
                    Log.Information($"loaded script: {type.Name}...");

                    break;
            }

            script.Unloading += delegate
            {
                scripts.Unbind(script);
            };

            scripts.Bind(script);
        }

        public void Tick()
        {
            foreach (var script in scripts.All<IApplicationScript>().ToList())
            {
                switch (script)
                {
                    case IApplicationCommandScript iacs:
                        try
                        {
                            iacs.Invoke();
                        }
                        catch (Exception e)
                        {
                            Log.Warning(e, "unhandled error occurred while invoking command script", script);
                        }

                        break;
                    case IActiveApplicationScript iaas:
                        try
                        {
                            iaas.Tick();
                        }
                        catch (Exception e)
                        {
                            Log.Warning(e, "unhandled error occurred while executing active script", script);
                        }

                        break;
                }
            }
        }

        public void Shutdown()
            => application.Shutdown();
    }

    /// <summary>
    /// Class that serves as application host in cases the service and script layer is used for application programming. This class groups application,
    /// services and scripts together.
    /// </summary>
    public class ApplicationHost
    {
        #region Properties
        protected Application Application
        {
            get;
        }

        protected ApplicationScriptingHost Scripts
        {
            get;
        }

        protected ApplicationServiceHost Services
        {
            get;
        }
        #endregion

        public ApplicationHost(Application application, ApplicationServiceHost services, ApplicationScriptingHost scripts)
        {
            Application = application ?? throw new ArgumentNullException(nameof(application));
            Services    = services ?? throw new ArgumentNullException(nameof(services));
            Scripts     = scripts ?? throw new ArgumentNullException(nameof(scripts));
        }

        /// <summary>
        /// Signals the application to shutdown.
        /// </summary>
        public void Shutdown()
            => Application.Shutdown();

        /// <summary>
        /// Starts running the application.
        /// </summary>
        public void Start()
        {
            Application.Tick += delegate
            {
                Services.Tick();

                Scripts.Tick();
            };

            Application.Start();
        }
    }
}