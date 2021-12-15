namespace Fracture.Net.Hosting.Services
{
    public interface IAuthenticationService<in T> : IApplicationService
    {
        bool Authenticate(T credentials);
    }
}