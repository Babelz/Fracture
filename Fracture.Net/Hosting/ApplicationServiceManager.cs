using System;
using Fracture.Common.Di;
using NLog;

namespace Fracture.Net.Hosting
{
    public interface IApplicationServiceManager
    {
        void Tick();
    }
    
    public sealed class ApplicationServiceManager : IApplicationServiceManager
    {
        #region Static fields
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion
        
        #region Fields
        private readonly IDependencyLocator locator;
        #endregion
        
        public ApplicationServiceManager(IDependencyLocator locator)
            => this.locator = locator ?? throw new ArgumentNullException(nameof(locator)); 
        
        public void Tick()
        {
            foreach (var service in locator.All<IActiveApplicationService>())
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
    }
}