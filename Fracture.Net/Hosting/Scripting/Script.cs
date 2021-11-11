using System;

namespace Fracture.Net.Hosting
{
    public interface IScriptActivationArgs
    {
    }
    
    public interface IScript
    {
        #region Events
        event EventHandler Unloading;
        #endregion
    }
    
    public interface ICommandScript : IScript
    {
        void Invoke(IApplicationClock clock);
    }
    
    public interface IActiveScript : IScript
    {
        void Tick(IApplicationClock clock); 
    }
    
    public abstract class Script : IScript
    {
        #region Fields
        private bool unloaded;
        #endregion
        
        #region Events
        public event EventHandler Unloading;
        #endregion
        
        #region Properties
        protected IApplicationScriptingHost Application
        {
            get;
        }
        
        protected IScriptActivationArgs Args
        {
            get;
        }
        #endregion

        public Script(IApplicationScriptingHost application, IScriptActivationArgs args = null)
        {
            Application = application ?? throw new ArgumentNullException(nameof(application));
            Args        = args;
        }
        
        public virtual void Unload()
        {
            if (unloaded)
                throw new InvalidOperationException("script is already unloaded");
            
            unloaded = true;
            
            Unloading?.Invoke(this, EventArgs.Empty);
        }
    }
    
    public abstract class CommandScript : Script, ICommandScript
    {
        protected CommandScript(IApplicationScriptingHost application, IScriptActivationArgs args = null) 
            : base(application, args)
        {
        }

        public virtual void Invoke(IApplicationClock clock)
        {
            Unload();
        }
    }
    
    public abstract class ActiveScript : Script, IActiveScript
    {
        protected ActiveScript(IApplicationScriptingHost application, IScriptActivationArgs args = null) 
            : base(application, args)
        {
        }

        public abstract void Tick(IApplicationClock clock);
    }
}