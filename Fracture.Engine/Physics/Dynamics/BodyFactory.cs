using Microsoft.Xna.Framework;

namespace Fracture.Engine.Physics.Dynamics
{
    public static class BodyFactory
    {
        public static Body CreateBox(BodyType type, Vector2 bounds, Vector2 position, float angle = 0.0f)
        {
            var body = new Body();
            
            body.Setup(type, ShapeFactory.CreateBox(bounds), position, angle);

            return body;
        }

        public static Body CreateCircle(BodyType type, float radius, Vector2 position)
        {
            var body = new Body();

            body.Setup(type, ShapeFactory.CreateCircle(radius), position);

            return body;
        }

        public static Body CreatePolygon(BodyType type, Vector2[] vertices, Vector2 position, float angle = 0.0f)
        {
            var body = new Body();

            body.Setup(type, ShapeFactory.CreatePolygon(vertices), position, angle);

            return body;
        }
    }
}
