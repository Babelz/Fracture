using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;

namespace Fracture.Engine.Physics.Dynamics
{
    public static class ShapeFactory
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Shape CreateCircle(float radius)
        {
            var circle = new CircleShape();

            circle.Setup(radius);

            return circle;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Shape CreateBox(Vector2 bounds)
        {
            var box = new PolygonShape();

            box.Setup(new[]
                      {
                          // Top left.
                          new Vector2(bounds.X * -0.5f, bounds.Y * -0.5f),

                          // Top right.
                          new Vector2(bounds.X * 0.5f, bounds.Y * -0.5f),

                          // Bottom right.
                          new Vector2(bounds.X * 0.5f, bounds.Y * 0.5f),

                          // Bottom left.
                          new Vector2(bounds.X * -0.5f, bounds.Y * 0.5f),
                      });

            return box;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Shape CreatePolygon(Vector2[] vertices)
        {
            var polygon = new PolygonShape();

            polygon.Setup(vertices);

            return polygon;
        }
    }
}
