using System;
using System.Collections.Generic;
using Fracture.Common.Di;
using Fracture.Common.Di.Attributes;
using NLog;

namespace Fracture.Net.Hosting
{
    /// <summary>
    /// Provides interface for creating application services. Services are reusable stateful objects that contain shared framework level code inside the
    /// application. Services should be thought to model framework level concepts such as session state containers, schedulers and database clients. Services
    /// should not be able to access the messaging pipeline or scripting host of the application. Services can communicate with each other directly if needed.
    ///
    /// Services are initialized once for the application and they are unloaded only when the application exists.
    /// </summary>
    public interface IApplicationService
    {
        // Marker interface, nothing to implement. Each service should provide it's functionality via public interface declaration.
    }
    
    /// <summary>
    /// Interface for implementing active services that are updated during each application tick. 
    /// </summary>
    public interface IActiveApplicationService : IApplicationService 
    {
        /// <summary>
        /// Allows the service to run updates.
        /// </summary>
        void Tick();
    }
    
    public interface IApplicationServiceManager
    {
        void Initialize(IDependencyLocator locator);
        
        void Tick();
    }
    
    public sealed class ApplicationServiceManager : IApplicationServiceManager
    {
        #region Static fields
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion
        
        #region Fields
        private readonly List<IActiveApplicationService> services;
        #endregion
        
        public ApplicationServiceManager()
            => services = new List<IActiveApplicationService>();
        
        public void Initialize(IDependencyLocator locator)
            => services.AddRange(locator.All<IActiveApplicationService>());
        
        public void Tick()
        {
            foreach (var service in services)
            {
                try
                {
                    service.Tick();
                }
                catch (Exception e)
                {
                    Log.Error(e, "error occurred while updating service", service);
                }   
            }
        }
    }

    /// <summary>
    /// Base class for implementing services.
    /// </summary>
    public abstract class ApplicationService
    {
        #region Properties
        protected IApplicationHost Application
        {
            get;
        }
        #endregion

        /// <summary>
        /// Creates new instance of <see cref="ApplicationService"/>. Use this constructor for locating any dependencies by annotating it with
        /// <see cref="BindingConstructorAttribute"/>.
        /// </summary>
        protected ApplicationService(IApplicationHost application)
            => Application = application ?? throw new ArgumentNullException(nameof(application));
    }
    
    /// <summary>
    /// Base class for implementing active services.
    /// </summary>
    public abstract class ActiveApplicationService : ApplicationService
    {
        protected ActiveApplicationService(IApplicationHost application) 
            : base(application)
        {
        }
        
        public abstract void Tick();
    }
}