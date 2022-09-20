using System;
using System.Runtime.CompilerServices;
using Fracture.Common.Di;
using Fracture.Common.Di.Binding;
using Serilog;

namespace Fracture.Net.Hosting
{
    /// <summary>
    /// Base class that provides common application host building functionality. Generality of this class makes no sense from functional point of view and it
    /// is only intended for reusing the functionality of the base builder in inheriting builders. 
    /// </summary>
    public abstract class BaseApplicationHostBuilder<TImplementation, TApplicationHost> where TImplementation : BaseApplicationHostBuilder<TImplementation, TApplicationHost>
                                                                                        where TApplicationHost : ApplicationHost
    {
        #region Properties
        protected Application Application
        {
            get;
        }

        protected Kernel Scripts
        {
            get;
        }

        protected Kernel Services
        {
            get;
        }
        #endregion

        protected BaseApplicationHostBuilder(Application application)
        {
            Application = application ?? throw new ArgumentNullException(nameof(application));

            Scripts  = new Kernel(DependencyBindingOptions.BaseType | DependencyBindingOptions.Interfaces);
            Services = new Kernel(DependencyBindingOptions.BaseType | DependencyBindingOptions.Interfaces);
        }

        /// <summary>
        /// Register service to be used by the application.
        /// </summary>
        public virtual TImplementation Service<T>(params IBindingValue [] args) where T : class, IApplicationService
        {
            Services.Bind<T>(args);

            return (TImplementation)this;
        }

        /// <summary>
        /// Register startup script that is loaded to the application before it starts.
        /// </summary>
        public virtual TImplementation Script<T>(params IBindingValue [] args) where T : class, IApplicationScript
        {
            Scripts.Bind<T>(args);

            return (TImplementation)this;
        }

        /// <summary>
        /// Register custom service dependency that can injected to services.
        /// </summary>
        public virtual TImplementation ServiceDependency(object dependency)
        {
            Services.Bind(dependency);

            return (TImplementation)this;
        }

        /// <summary>
        /// Register custom script dependency that can injected to scripts.
        /// </summary>
        public virtual TImplementation ScriptDependency(object dependency)
        {
            Scripts.Bind(dependency);

            return (TImplementation)this;
        }

        /// <summary>
        /// Registers custom dependency for both scripts and services to use.
        /// </summary>
        public TImplementation SharedDependency(object dependency)
            => ScriptDependency(dependency).ServiceDependency(dependency);

        public abstract TApplicationHost Build();
    }

    /// <summary>
    /// Class that provides builder implementation for <see cref="ApplicationHost"/>.
    /// </summary>
    public sealed class ApplicationHostBuilder : BaseApplicationHostBuilder<ApplicationHostBuilder, ApplicationHost>
    {
        private ApplicationHostBuilder(Application application) 
            : base(application)
        {
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void LogBinding(Type type, string asWhat = "")
            => Log.Information($"binding {type.FullName} to application host builder{(string.IsNullOrEmpty(asWhat) ? "..." : $" as {asWhat}...")}");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void LogBinding(object value, string asWhat = "")
            => LogBinding(value.GetType(), asWhat);

        public override ApplicationHostBuilder Service<T>(params IBindingValue [] args) 
        {
            LogBinding(typeof(T), "as service");

            return base.Service<T>(args);
        }

        public override ApplicationHostBuilder Script<T>(params IBindingValue [] args)
        {
            LogBinding(typeof(T), "as script");

            return base.Script<T>(args);
        }

        public override ApplicationHostBuilder ServiceDependency(object dependency)
        {
            LogBinding(dependency, "as service dependency");

            return base.ServiceDependency(dependency);
        }

        public override ApplicationHostBuilder ScriptDependency(object dependency)
        {
            LogBinding(dependency, "as script dependency");

            return base.ScriptDependency(dependency);
        }
        
        public override ApplicationHost Build()
        {
            Log.Information("building application host");

            return new ApplicationHost(Application,
                                       new ApplicationServiceHost(Services, Application),
                                       new ApplicationScriptingHost(Scripts, Application, Services.All<IApplicationService>()));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ApplicationHostBuilder FromApplication(Application application)
            => new ApplicationHostBuilder(application);
    }
}