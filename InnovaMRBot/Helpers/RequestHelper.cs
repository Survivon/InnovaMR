using InnovaMRBot.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TelegramBotApi.Models.Keyboard;
using TelegramBotApi.Telegram.Request;

namespace InnovaMRBot.Helpers
{
    public static class RequestHelper
    {
        private const string TICKET_NUMBER_PATTERN = @"\w+-[0-9]+";

        public static void AddButtonForRequest(this SendMessageRequest message, string mrLink, List<string> ticketLinks, int okCount = 0, int badCount = 0, int watchCount = 0)
        {
            var lineButton = new List<InlineKeyboardButton>()
            {
                new InlineKeyboardButton()
                {
                    Text = "👍" + (okCount == 0 ? string.Empty : $" ({okCount})"),
                    CallbackData = Glossary.InlineAction.SUCCESS_REACTION,
                },
                new InlineKeyboardButton()
                {
                    Text = "MR",
                    Url = mrLink,
                },
                new InlineKeyboardButton()
                {
                    Text = "🚫" + (badCount == 0 ? string.Empty : $" ({badCount})"),
                    CallbackData = Glossary.InlineAction.BAD_REACTION,
                },
                new InlineKeyboardButton()
                {
                    Text = "🔎" + (watchCount == 0 ? string.Empty : $" ({watchCount})"),
                    CallbackData = Glossary.InlineAction.START_WATCH,
                },
            };

            var ticketButtons = new List<InlineKeyboardButton>
            {
                new InlineKeyboardButton()
                {
                    Text = "📈",
                    CallbackData = Glossary.InlineAction.GET_STAT,
                },
            };

            foreach (var ticketLink in ticketLinks.Where(c => !string.IsNullOrEmpty(c)).Take(2))
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

            ticketLinks = ticketLinks.Skip(2).ToList();

            for (int i = 0; i < ticketLinks.Count; i++)
            {
                if (i % 3 == 0)
                {
                    buttons.Add(new List<InlineKeyboardButton>());
                }

                var text = Regex.Match(ticketLinks[i], TICKET_NUMBER_PATTERN).Value;
                buttons.LastOrDefault().Add(new InlineKeyboardButton()
                {
                    Text = text,
                    Url = ticketLinks[i],
                });
            }

            message.ReplyMarkup = new InlineKeyboardMarkup()
            {
                InlineKeyboardButtons = buttons,
            };
        }
    }
}
