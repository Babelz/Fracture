namespace Fracture.Net.Hosting.Services
{
    public interface IServiceLocator
    {
        T Locate<T>() where T : IService;
    }
    
    public interface IServiceContainer : IServiceLocator
    {
        void Tick(IApplicationClock clock);
    }
}