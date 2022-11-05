namespace Fracture.Engine
{
    /// <summary>
    /// Static utility class that contains collection of event related configuration for the Fracture game engine.
    ///
    /// TODO: wip, still conceptual idea. Was planning the following with this:
    /// - to decouple some systems from their interfaces, we can use the event queue
    /// - some system are interested about other systems only because of the events they provide
    /// - instead of subscribing directly to the system interface, use the event queue instead
    /// - this would make the event queue more significant part of the game design process, currently the queue is only used for ECS events
    /// </summary>
    public static class GameHostEvents
    {
        /// <summary>
        /// Static class that defines event topics for the Fracture engine. Events of the fracture engine use objects as their topic
        /// keys as the integer topics space is reserved for ECS. 
        /// </summary>
        public static class Topics
        {
            /// <summary>
            /// Static class that contains event topics for graphics related events.
            /// </summary>
            public static class Graphics
            {
                #region Static fields
                public static object BackbufferSizeChanged = new object();
                public static object ViewportChanged       = new object();
                #endregion
            }
            
            /// <summary>
            /// Static class that contains event topics for net related events.
            /// </summary>
            public static class Net
            {
                #region Static fields
                public static object Reset = new object();
                #endregion
            }
        }
    }
}