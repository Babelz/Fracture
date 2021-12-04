using Fracture.Common.Di.Attributes;
using Fracture.Net.Hosting;

namespace Fracture.Net.Tests.Hosting.Utils
{
    /// <summary>
    /// Testing utility script for limiting the execution time of applications when testing.
    /// </summary>
    public class TickLimiterScript : ActiveApplicationScript
    {
        #region Fields
        private readonly ulong limit;
        #endregion

        /// <summary>
        /// Creates new instance of <see cref="TickLimiterScript"/>.
        /// </summary>
        /// <param name="application">application which tick counts this script will be limiting</param>
        /// <param name="limit">after how many ticks are executed before the <see cref="Application.Shutdown"/> method is called</param>
        [BindingConstructor]
        public TickLimiterScript(IApplicationScriptingHost application, ulong limit) 
            : base(application)
        {
            this.limit = limit;
        }

        public override void Tick()
        {
            if (Application.Clock.Ticks >= limit)
                Application.Shutdown();
        }
    }
}