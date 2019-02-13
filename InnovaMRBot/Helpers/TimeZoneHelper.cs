using System;
using System.Collections.Generic;
using InnovaMRBot.Models;

namespace InnovaMRBot.Helpers
{
    public static class TimeZoneHelper
    {
        public static Dictionary<string, long> Time = new Dictionary<string, long>()
        {
            { "Ukraine", 2 },
            { "Belarus", 3 },
            { "Russia", 3 },
            { "France", 1 },
            { "UTC", 0 },
        };

        public static string GetUserTime(this DateTimeOffset time, User user)
        {
            return $"{time.AddHours(user.TimeDiff):MM/dd/yy H:mm:ss}";
        }
    }
}
