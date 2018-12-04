using System;

namespace InnovaMRBot.Helpers
{
    public static class MinutesConverter
    {
        public static string MinutesToCorrectTimeConverter(this int minutes)
        {
            var result = string.Empty;

            var timeSpan = TimeSpan.FromMinutes(minutes);

            if (timeSpan.Days > 0)
            {
                result += $"{timeSpan.Days} days ";
            }

            if (timeSpan.Hours > 0)
            {
                result += $"{timeSpan.Hours} hour(s) ";
            }

            if (timeSpan.Minutes > 0)
            {
                result += $"{timeSpan.Minutes} min ";
            }

            return result;
        }
    }
}
