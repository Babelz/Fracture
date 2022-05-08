using System;
using System.Runtime.CompilerServices;
using Fracture.Common.Di;
using Fracture.Common.Di.Binding;
using Serilog;

namespace Fracture.Net.Hosting
{
    /// <summary>
    /// Class that provides application host building functionality.
    /// </summary>
    public sealed class ApplicationHostBuilder
    {
        #region Fields
        private readonly Application application;
        
        private readonly Kernel scripts;
        private readonly Kernel services;
        #endregion
        
        private ApplicationHostBuilder(Application application)
        {
            this.application = application ?? throw new ArgumentNullException(nameof(application));
            
            scripts  = new Kernel(DependencyBindingOptions.BaseType | DependencyBindingOptions.Interfaces);
            services = new Kernel(DependencyBindingOptions.BaseType | DependencyBindingOptions.Interfaces);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void LogBinding(Type type, string asWhat = "")
            => Log.Information($"binding {type.FullName} to application host builder{(string.IsNullOrEmpty(asWhat) ? "..." : $" as {asWhat}...")}");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void LogBinding(object value, string asWhat = "")
            => LogBinding(value.GetType(), asWhat);
        
        /// <summary>
        /// Register service to be used by the application.
        /// </summary>
        public ApplicationHostBuilder Service<T>(params IBindingValue[] args) where T : class, IApplicationService
        {
            LogBinding(typeof(T), "as service");
            
            services.Bind<T>(args);
            
            return this;
        }
        
        /// <summary>
        /// Register startup script that is loaded to the application before it starts.
        /// </summary>
        public ApplicationHostBuilder Script<T>(params IBindingValue[] args) where T : class, IApplicationScript
        {
            LogBinding(typeof(T), "as script");

            scripts.Bind<T>(args);
            
            return this;
        }
        
        /// <summary>
        /// Register custom service dependency that can injected to services.
        /// </summary>
        public ApplicationHostBuilder ServiceDependency(object dependency)
        {
            LogBinding(dependency, "as service dependency");

            services.Bind(dependency);
            
            return this;
        }
        
        /// <summary>
        /// Register custom script dependency that can injected to scripts.
        /// </summary>
        public ApplicationHostBuilder ScriptDependency(object dependency)
        {
            LogBinding(dependency, "as script dependency");

            scripts.Bind(dependency);
            
            return this;
        }
        
        /// <summary>
        /// Registers custom dependency for both scripts and services to use.
        /// </summary>
        public ApplicationHostBuilder SharedDependency(object dependency)
            => ScriptDependency(dependency).ServiceDependency(dependency);
        
        public ApplicationHost Build()
        {
            Log.Information("building application host"); 
            
            return new ApplicationHost(application, 
                                       new ApplicationServiceHost(services, application),
                                       new ApplicationScriptingHost(scripts, application, services.All<IApplicationService>()));
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ApplicationHostBuilder FromApplication(Application application)
            => new ApplicationHostBuilder(application);
    }
}