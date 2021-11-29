using System;
using Fracture.Common.Di.Attributes;

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

    /// <summary>
    /// Base class for implementing services.
    /// </summary>
    public abstract class ApplicationService
    {
        #region Properties
        protected IApplicationServiceHost Application
        {
            get;
        }
        #endregion

        /// <summary>
        /// Creates new instance of <see cref="ApplicationService"/>. Use this constructor for locating any dependencies by annotating it with
        /// <see cref="BindingConstructorAttribute"/>.
        /// </summary>
        protected ApplicationService(IApplicationServiceHost application)
            => Application = application ?? throw new ArgumentNullException(nameof(application));
    }
    
    /// <summary>
    /// Base class for implementing active services.
    /// </summary>
    public abstract class ActiveApplicationService : ApplicationService
    {
        protected ActiveApplicationService(IApplicationServiceHost application) 
            : base(application)
        {
        }
        
        public abstract void Tick();
    }
}