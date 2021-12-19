using System;
using Fracture.Common.Di.Attributes;

namespace Fracture.Net.Hosting
{
    /// <summary>
    /// Provides interface for creating application services. Services are reusable stateful objects that function as the model part in applications. Services
    /// can communicate to each other and should have limited access to application. 
    ///
    /// Services are the model part in any application and services are initialized once for the application and they are unloaded only when the application
    /// exists.
    /// </summary>
    public interface IApplicationService
    {
        // Marker interface, nothing to implement. Each service should provide its functionality via public interface declaration.
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
    /// Abstract base class for creating application services.
    /// </summary>
    public abstract class ApplicationService : IApplicationService
    {
        #region Properties
        protected IApplicationServiceHost Application
        {
            get;
        }
        #endregion

        /// <summary>
        /// Creates new instance of this service. Use this constructor for locating any dependencies by annotating it with <see cref="BindingConstructorAttribute"/>.
        /// </summary>
        protected ApplicationService(IApplicationServiceHost application)
            => Application = application ?? throw new ArgumentNullException(nameof(application));
    }
    
    /// <summary>
    /// Abstract base class for implementing active services that are updated during each application tick. 
    /// </summary>
    public abstract class ActiveApplicationService : ApplicationService, IActiveApplicationService
    {
        /// <summary>
        /// Creates new instance of this service. Use this constructor for locating any dependencies by annotating it with <see cref="BindingConstructorAttribute"/>.
        /// </summary>
        protected ActiveApplicationService(IApplicationServiceHost application) 
            : base(application)
        {
        }
        
        /// <summary>
        /// Allows the service to run updates.
        /// </summary>
        public abstract void Tick();
    }
}