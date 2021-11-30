using System;
using System.Collections.Generic;
using System.Linq;
using Fracture.Common.Di;
using Fracture.Common.Di.Binding;
using Fracture.Common.Events;
using NLog;

namespace Fracture.Net.Hosting
{
    /// <summary>
    /// Interface that provides functionality for loading new scripts during runtime inside the application.
    /// </summary>
    public interface IApplicationScriptLoader
    {   
        void Load<T>(params IBindingValue[] args) where T : class, IApplicationScript;
    }
    
    public sealed class ApplicationScriptLoader : IApplicationScriptLoader
    {
        #region Static fields
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion
        
        #region Fields
        private readonly Kernel kernel;
        #endregion
        
        public ApplicationScriptLoader(Kernel kernel)
        {
            this.kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
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
            
            var script = (IApplicationScript)kernel.Activate(typeof(T), args);
            
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
            
            script.Unloading += delegate
            {
                kernel.Unbind(script);
            };
            
            kernel.Bind(script);
        }
    }
    
    public sealed class ApplicationKernel
    {
        #region Static fields
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion
        
        #region Fields
        private readonly Kernel services;
        private readonly Kernel scripts;
        #endregion
        
        public ApplicationKernel(Application application, Kernel services, Kernel scripts)
        {
            if (application == null)
                throw new ArgumentNullException(nameof(application));

            this.services = services ?? throw new ArgumentNullException(nameof(services));
            this.scripts  = scripts ?? throw new ArgumentNullException(nameof(scripts));

            // Bind service proxies.
            services.Proxy(application, typeof(IApplicationServiceHost));
            
            // Bind services to scripts.
            foreach (var service in services.All())
                scripts.Bind(service);
            
            // Bind script proxies.
            scripts.Proxy(application, typeof(IApplicationScriptingHost));
            scripts.Proxy(new ApplicationScriptLoader(scripts), typeof(IApplicationScriptLoader));
        }
        
        private void UpdateServices()
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
        
        private void UpdateScripts()
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
        
        public void Tick()
        {
            UpdateServices();
            
            UpdateScripts();
        }
    }
}