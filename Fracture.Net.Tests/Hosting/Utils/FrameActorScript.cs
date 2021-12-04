using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Fracture.Common.Di.Attributes;
using Fracture.Net.Hosting;

namespace Fracture.Net.Tests.Hosting.Utils
{
    public readonly struct FrameAction
    {
        #region Properties
        public ulong Frame
        {
            get;
        }

        public Action<IApplicationScriptingHost> Callback
        {
            get;
        }
        #endregion

        public FrameAction(ulong frame, Action<IApplicationScriptingHost> callback)
        {
            Frame    = frame;
            Callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FrameAction Create(ulong frame, Action<IApplicationScriptingHost> callback) 
            => new FrameAction(frame, callback);
    }
    
    public sealed class FrameActorScript : ActiveApplicationScript
    {
        #region Fields
        private readonly IEnumerable<FrameAction> actions;
        #endregion
        
        [BindingConstructor]
        public FrameActorScript(IApplicationScriptingHost application, params FrameAction[] actions) 
            : base(application)
        {
            this.actions = actions;
        }

        public override void Tick()
            => actions.Where(a => a.Frame == Application.Clock.Ticks)
                      .ToList()
                      .ForEach(a => a.Callback(Application));
    }
}