using System;
using Fracture.Engine.Physics.Dynamics;

namespace Fracture.Engine.Physics
{
    public sealed class BodyContactEventArgs : EventArgs
    {
        #region Properties
        public Body Body
        {
            get;
            set;
        }

        public Body Contact
        {
            get;
            set;
        }
        #endregion

        public BodyContactEventArgs()
        {
        }
    }

    public sealed class WorldEventArgs : EventArgs
    {
        #region Properties
        public PhysicsWorldSystem PhysicsWorldSystem
        {
            get;
        } 
        #endregion

        public WorldEventArgs(PhysicsWorldSystem physicsWorldSystem)
            => PhysicsWorldSystem = physicsWorldSystem;
    }
}
