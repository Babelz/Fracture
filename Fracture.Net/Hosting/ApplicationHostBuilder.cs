using System;
using System.Runtime.CompilerServices;
using Fracture.Common.Di;
using Fracture.Common.Di.Binding;

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
            
            scripts  = new Kernel(DependencyBindingOptions.Class | DependencyBindingOptions.Interfaces);
            services = new Kernel(DependencyBindingOptions.Interfaces);
        }
        
        /// <summary>
        /// Register service to be used by the application.
        /// </summary>
        public ApplicationHostBuilder Service<T>(params IBindingValue[] args) where T : class, IApplicationService
        {
            services.Bind<T>(args);
            
            return this;
        }
        
        /// <summary>
        /// Register startup script that is loaded to the application before it starts.
        /// </summary>
        public ApplicationHostBuilder Script<T>(params IBindingValue[] args) where T : class, IApplicationScript
        {
            scripts.Bind<T>(args);
            
            return this;
        }
        
        /// <summary>
        /// Register custom service dependency that can injected to services.
        /// </summary>
        public ApplicationHostBuilder ServiceDependency(object dependency)
        {
            services.Bind(dependency ?? throw new ArgumentNullException(nameof(dependency)));
            
            return this;
        }
        
        /// <summary>
        /// Register custom script dependency that can injected to scripts.
        /// </summary>
        public ApplicationHostBuilder ScriptDependency(object dependency)
        {
            scripts.Bind(dependency ?? throw new ArgumentNullException(nameof(dependency)));
            
            return this;
        }
        
        /// <summary>
        /// Registers custom dependency for both scripts and services to use.
        /// </summary>
        public ApplicationHostBuilder SharedDependency(object dependency)
            => ScriptDependency(dependency).ServiceDependency(dependency);
        
        public ApplicationHost Build()
            => new ApplicationHost(application, 
                                   new ApplicationScriptingHost(scripts, application, services.All<IApplicationService>()), 
                                   new ApplicationServiceHost(services, application));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ApplicationHostBuilder FromApplication(Application application)
            => new ApplicationHostBuilder(application);
    }
}