using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;

namespace Fracture.Engine.Util
{
    public static class VectorUtil
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ToUnit(Vector2 value)
        {
            static float ToUnit(float value)
            {
                if (value > 0.0f)
                    return 1.0f;
                
                if (value < 0.0f)
                    return -1.0f;

                return 0.0f;
            }
            
            return new Vector2(ToUnit(value.X), ToUnit(value.Y));
        }
    }
}