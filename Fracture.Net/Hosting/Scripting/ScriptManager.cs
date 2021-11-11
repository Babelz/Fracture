namespace Fracture.Net.Hosting
{
    public interface IScriptHost
    {
        void Load<T>(IScriptActivationArgs args) where T : IScript;
    }
    
    public interface IScriptManager : IScriptHost
    {
        void Tick(IApplicationClock clock);
    }
}