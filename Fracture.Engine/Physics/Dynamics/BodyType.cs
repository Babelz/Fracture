namespace Fracture.Engine.Physics.Dynamics
{
    /// <summary>
    /// Enumeration defining supported body types.
    /// </summary>
    public enum BodyType : byte
    {
        /// <summary>
        /// Undefined body type.
        /// </summary>
        None = 0,

        /// <summary>
        /// Static bodies cannot be moved and collide with dynamic bodies.
        /// </summary>
        Static = 1,

        /// <summary>
        /// Dynamic bodies are moved by their velocity or position and collide with static bodies
        /// and respond to collisions.
        /// </summary>
        Dynamic = 2,

        /// <summary>
        /// Sensor bodies are moved by their velocity or position and only detect collisions
        /// with dynamic bodies but do not respond to them.
        /// </summary>
        Sensor = 3
    }
}
