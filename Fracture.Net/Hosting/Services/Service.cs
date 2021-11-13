namespace Fracture.Net.Hosting.Services
{
    public interface IService 
    {
    }
    
    public abstract class Service
    {
        public Service(IApplicationServiceHost application)
        {
        }
    }
}