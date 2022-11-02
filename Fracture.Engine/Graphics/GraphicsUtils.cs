using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;

namespace Fracture.Engine.Graphics
{
    public static class GraphicsUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ActualScale(in Vector2 fromBounds, in Vector2 toBounds, in Vector2 scale)
        {
            var (ax, ay) = fromBounds;
            var (tx, ty) = toBounds;

            var rx = 0.0f;
            var ry = 0.0f;

            if (tx < ax)
                rx = ax / tx;
            else
                rx = tx / ax;

            if (ty < ay)
                ry = ay / ty;
            else
                ry = ty / ay;

            var actual = new Vector2(scale.X / rx, scale.Y / ry);

            return actual;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ScaleToTarget(in Vector2 target, in Vector2 actual)
            => new Vector2(target.X / actual.X, target.Y / actual.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 OriginInActualScale(in Vector2 origin, in Vector2 actualScale)
            => new Vector2(origin.X / actualScale.X, origin.Y / actualScale.Y);
    }
}