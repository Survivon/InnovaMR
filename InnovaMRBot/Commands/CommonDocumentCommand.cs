using System.Collections.Generic;
using System.Threading.Tasks;
using InnovaMRBot.Repository;
using InnovaMRBot.Services;
using TelegramBotApi.Extension;
using TelegramBotApi.Models;
using TelegramBotApi.Models.Keyboard;
using TelegramBotApi.Telegram;
using TelegramBotApi.Telegram.Request;

namespace InnovaMRBot.Commands
{
    public class CommonDocumentCommand : BaseCommand
    {
        private const string COMMAND = "/getcommondocument";

        public CommonDocumentCommand(Telegram telegram, UnitOfWork dbContext, Logger logger) : base(telegram, dbContext, logger)
        {
            CommandId = "commondocumentcommand";
        }
        
        protected override string GetCommandString()
        {
            return COMMAND;
        }

        public override async Task WorkerAsync(Update update)
        {
            _telegram.SendMessageAsync(new SendMessageRequest
            {
                Text = "Document Link",
                ChatId = update.Message.Chat.Id.ToString(),
                ReplyMarkup = new InlineKeyboardMarkup
                {
                    InlineKeyboardButtons = new List<List<InlineKeyboardButton>>
                    {
                        new List<InlineKeyboardButton>
                        {
                            new InlineKeyboardButton
                            {
                                Text = "Link",
                                Url =
                                    "https://docs.google.com/document/d/1MNI8ZY-Fciqk6q7PZnJz2aDQe4TllQHsdOo6jpim_9s/edit",
                            },
                        },
                    },
                },
            }).ConfigureAwait(false);
        }
    }
}
