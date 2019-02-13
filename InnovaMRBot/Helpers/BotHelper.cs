using System;
using System.Globalization;
using TelegramBotApi.Models;

namespace InnovaMRBot.Helpers
{
    public static class BotHelper
    {
        public static DateTime ConvertToDate(this string date)
        {
            DateTimeFormatInfo dtfi = CultureInfo.CreateSpecificCulture("en-US").DateTimeFormat;
            return Convert.ToDateTime(date, new DateTimeFormatInfo
            {
                ShortDatePattern = dtfi.ShortDatePattern,
            });
        }

        public static string GetUserFullName(this User user)
        {
            return $"{user.FirstName} {user.LastName}";
        }
    }
}
