using System;

namespace RLBot.API.RLS.Util
{
    internal static class TimeUtil
    {
        private static DateTimeOffset _epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, 0, TimeSpan.Zero);

        public static DateTimeOffset UnixTimeStampToDateTime(double unixTimeStamp)
        {
            return _epoch.AddSeconds(unixTimeStamp);
        }
    }
}