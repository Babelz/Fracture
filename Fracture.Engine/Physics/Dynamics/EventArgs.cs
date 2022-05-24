using System;

namespace Fracture.Engine.Physics.Dynamics
{
    public sealed class BodyEventArgs
    {
        #region Properties
        public Body Body
        {
            get;
        } 
        #endregion

        public BodyEventArgs(Body body)
            => Body = body ?? throw new ArgumentNullException(nameof(body));
    }
}
