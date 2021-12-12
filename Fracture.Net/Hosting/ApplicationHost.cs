using System;
using System.Linq;
using Fracture.Common.Di;
using Fracture.Common.Di.Binding;
using Fracture.Net.Hosting.Messaging;
using Fracture.Net.Hosting.Servers;
using NLog;

namespace Fracture.Net.Hosting
{
    /// <summary>
    /// Interface that provides common application host schematics between different host types. Application hosts extend on the application and provide
    /// model view controller style for application programming.
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
        event EventHandler<PeerJoinEventArgs> Join;
        
        /// <summary>
        /// Event invoked when peer has reset.
        /// </summary>
        event EventHandler<PeerResetEventArgs> Reset; 
        #endregion
        
        #region Properties
        /// <summary>
        /// Gets the application request context for working with the request pipeline.
        /// </summary>
        ApplicationRequestContext Request
        {
            get;
        }

        /// <summary>
        /// Gets the application notification context for working with notification pipeline.
        /// </summary>
        ApplicationNotificationContext Notification
        {
            get;
        }
        
        /// <summary>
        /// Gets the application response middleware consumer.
        /// </summary>
        IMiddlewareConsumer<RequestResponseMiddlewareContext> Response
        {
            get;
        }
        #endregion
        
        void Load<T>(params IBindingValue[] args) where T : class, IApplicationScript;
    }

    /// <summary>
    /// Default implementation of <see cref="IApplicationServiceHost"/>.
    /// </summary>
    public sealed class ApplicationServiceHost : IApplicationServiceHost
    {
        #region Static fields
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion

        #region Fields
        private readonly Application application;
        
        private readonly IDependencyLocator services;
        #endregion
        
        #region Events
        public event EventHandler Starting;
        public event EventHandler ShuttingDown;
        #endregion
        
        #region Properties
        public IApplicationClock Clock
            => application.Clock;
        #endregion
        
        public ApplicationServiceHost(Application application, Kernel services)
        {
            this.application = application ?? throw new ArgumentNullException(nameof(application));
            this.services    = services ?? throw new ArgumentNullException(nameof(services));
            
            services.Bind(this);
            
            application.ShuttingDown += delegate { ShuttingDown?.Invoke(this, EventArgs.Empty); };
            application.Starting     += delegate { Starting?.Invoke(this, EventArgs.Empty); };
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
                    Log.Warn(e, "unhandled error occurred while updating service", service);
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
        #region Static fields
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion
        
        #region Fields
        private readonly Application application;
        
        private readonly Kernel scripts;
        #endregion

        #region Events
        public event EventHandler<PeerJoinEventArgs> Join;
        public event EventHandler<PeerResetEventArgs> Reset;
        #endregion
        
        #region Properties
        public ApplicationRequestContext Request
            => application.Request;

        public ApplicationNotificationContext Notification
            => application.Notification;

        public IMiddlewareConsumer<RequestResponseMiddlewareContext> Response
            => application.Response;
        
        public IApplicationClock Clock
            => application.Clock;
        #endregion
        
        public ApplicationScriptingHost(Application application, IDependencyLocator services, Kernel scripts)
        {
            this.application = application ?? throw new ArgumentNullException(nameof(application));
            this.scripts     = scripts ?? throw new ArgumentNullException(nameof(scripts));
            
            // Bind services to scripts.
            foreach (var service in services.All())
                scripts.Bind(service);
            
            // Bind host to kernel.
            scripts.Bind(this);
            
            // Unload all scripts when the application exists.
            application.ShuttingDown += delegate
            {
                foreach (var script in scripts.All<IApplicationScript>().ToList())
                {
                    try
                    {
                        script.Unload();
                    }
                    catch (Exception ex)
                    {
                        Log.Warn(ex, "unhandled error occurred while unloading script");
                    }
                }
            };
            
            application.Join  += (s, e) => Join?.Invoke(this, e);
            application.Reset += (s, e) => Reset?.Invoke(this, e);
        }
        
        public void Load<T>(params IBindingValue[] args) where T : class, IApplicationScript
        {
            var type = typeof(T);
            
            if (!typeof(IApplicationScript).IsAssignableFrom(type))
                throw new ArgumentException($"{type.Name} is not a script type", nameof(type));
                
            if (type.IsAbstract || type.IsInterface)
                throw new ArgumentException($"{type.Name} is abstract", nameof(type));
                
            if (type.IsValueType)
                throw new ArgumentException($"{type.Name} is a value type", nameof(type)); 
            
            var script = (IApplicationScript)scripts.Activate(typeof(T), args);
            
            switch (script)
            {
                case IApplicationCommandScript ics:
                    Log.Info($"loaded command script: {type.Name}...");
                    break;
                case IActiveApplicationScript ias:
                    Log.Info($"loaded active script: {type.Name}...");
                    break;
                default:
                    Log.Info($"loaded script: {type.Name}...");
                    break;
            }
            
            script.Unloading += delegate { scripts.Unbind(script); };
            
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
                            Log.Warn(e, "unahdled error occurred while invoking command script", script);
                        }
                        break;
                    case IActiveApplicationScript iaas:
                        try
                        {
                            iaas.Tick();
                        }
                        catch (Exception e)
                        {
                            Log.Warn(e, "unahdled error occurred while executing active script", script);
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
    public sealed class ApplicationHost
    {
        #region Fields
        private readonly Application application;
        
        private readonly ApplicationScriptingHost scripts;
        private readonly ApplicationServiceHost services;
        #endregion
        
        public ApplicationHost(Application application, ApplicationScriptingHost scripts, ApplicationServiceHost services)
        {
            this.application = application ?? throw new ArgumentNullException(nameof(application));
            this.scripts     = scripts ?? throw new ArgumentNullException(nameof(scripts));
            this.services    = services ?? throw new ArgumentNullException(nameof(services));
        }
        
        /// <summary>
        /// Signals the application to shutdown.
        /// </summary>
        public void Shutdown()
            => application.Shutdown();
        
        /// <summary>
        /// Starts running the application.
        /// </summary>
        public void Start()
        {
            application.Tick += delegate
            {
                services.Tick();
                
                scripts.Tick();
            };
                
            application.Start();
        }
    }
}