using System;

namespace Sdl.Web.Common.Utils
{
    /// <summary>
    /// Helper class for dealing with time outs
    /// </summary>
    public static class TimeOut
    {
        /// <summary>
        /// Returns the current time in milliseconds since system was started.
        /// </summary>
        public static uint GetTime() => (uint)Environment.TickCount;

        /// <summary>
        /// Returns the number of milliseconds left until wait time has elapsed
        /// </summary>
        /// <param name="startTime">Start time</param>
        /// <param name="originalWaitMillisecondsTimeout">Wait time in milliseconds</param>
        /// <returns>Time in milliseconds left until wait time has elapsed.</returns>
        public static int UpdateTimeOut(uint startTime, int originalWaitMillisecondsTimeout)
        {
            var elapsed = GetTime() - startTime;
            if (elapsed > int.MaxValue) return 0;
            var remaining = originalWaitMillisecondsTimeout - (int)elapsed;
            return remaining <= 0 ? 0 : remaining;
        }
    }
}
