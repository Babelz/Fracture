using System;
using System.Runtime.CompilerServices;

namespace Fracture.Common.Util
{
    /// <summary>
    /// Static utility class containing time related utilities.
    /// </summary>
    public static class TimeUtil
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TimeSpan TimeRemaining(DateTime startTime, DateTime timeNow, TimeSpan period)
            => TimeSpan.FromTicks(MathUtil.Clamp((int)(period - (timeNow - startTime)).Ticks, 0, int.MaxValue));
    }
}