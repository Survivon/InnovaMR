using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TelegramBotApi.Models.Keyboard;
using TelegramBotApi.Telegram.Request;

namespace InnovaMRBot.Helpers
{
    public static class RequestHelper
    {
        private const string TICKET_NUMBER_PATTERN = @"\w+-[0-9]+";

        public static void AddButtonForRequest(this SendMessageRequest message, string mrLink, List<string> ticketLinks, int okCount = 0, int badCount = 0)
        {
            var lineButton = new List<InlineKeyboardButton>()
            {
                new InlineKeyboardButton()
                {
                    Text = "👍" + (okCount == 0 ? string.Empty : $" ({okCount})"),
                    CallbackData = $"/success reaction",
                },
                new InlineKeyboardButton()
                {
                    Text = "MR",
                    Url = mrLink,
                    CallbackData = "/mr link open",
                },
                new InlineKeyboardButton()
                {
                    Text = "🚫" + (badCount == 0 ? string.Empty : $" ({badCount})"),
                    CallbackData = @"/bad reaction",
                },
                new InlineKeyboardButton()
                {
                    Text = "👁",
                    CallbackData = @"/start watch",
                },
            };

            var ticketButtons = new List<InlineKeyboardButton>
            {
                new InlineKeyboardButton()
                {
                    Text = "Stat 📈",
                    CallbackData = "/get stat",
                },
            };

            foreach (var ticketLink in ticketLinks.Where(c => !string.IsNullOrEmpty(c)))
            {
                var text = Regex.Match(ticketLink, TICKET_NUMBER_PATTERN).Value;
                ticketButtons.Add(new InlineKeyboardButton()
                {
                    Text = text,
                    Url = ticketLink,
                });
            }

            var buttons = new List<List<InlineKeyboardButton>>()
            {
                new List<InlineKeyboardButton>(lineButton),
                new List<InlineKeyboardButton>(ticketButtons),
            };

            message.ReplyMarkup = new InlineKeyboardMarkup()
            {
                InlineKeyboardButtons = buttons,
            };
        }
    }
}
