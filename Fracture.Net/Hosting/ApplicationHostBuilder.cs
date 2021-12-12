using System;
using System.Runtime.CompilerServices;
using Fracture.Common.Di;
using Fracture.Common.Di.Binding;

namespace Fracture.Net.Hosting
{
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
            services.Bind(dependency);
            
            return this;
        }
        
        /// <summary>
        /// Register custom script dependency that can injected to scripts.
        /// </summary>
        public ApplicationHostBuilder ScriptDependency(object dependency)
        {
            scripts.Bind(dependency);
            
            return this;
        }

        public ApplicationHost Build()
            => new ApplicationHost(application, 
                                   new ApplicationScriptingHost(application, services, scripts), 
                                   new ApplicationServiceHost(application, services));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ApplicationHostBuilder FromApplication(Application application)
            => new ApplicationHostBuilder(application);
    }
}