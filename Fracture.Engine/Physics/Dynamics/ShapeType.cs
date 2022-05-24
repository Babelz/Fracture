namespace Fracture.Engine.Physics.Dynamics
{
    /// <summary>
    /// Enumeration that defines supported shape types. This is a hint
    /// about the shapes type and is used to speed up solver lookups to
    /// avoid reflection.
    /// </summary>
    public enum ShapeType : byte
    {
        /// <summary>
        /// Undefined shape type.
        /// </summary>
        None = 0,
        
        /// <summary>
        /// Circle shape with 1 vertex.
        /// </summary>
        Circle = 1,

        /// <summary>
        /// Polygon shape with n-vertices.
        /// </summary>
        Polygon = 2
    }
}
