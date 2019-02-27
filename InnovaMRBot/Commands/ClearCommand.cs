using InnovaMRBot.Repository;
using System.Threading.Tasks;
using InnovaMRBot.Services;
using TelegramBotApi.Extension;
using TelegramBotApi.Models;
using TelegramBotApi.Models.Keyboard;
using TelegramBotApi.Telegram;
using TelegramBotApi.Telegram.Request;

namespace InnovaMRBot.Commands
{
    public class ClearCommand : BaseCommand
    {
        public const string COMMAND = "/cancel";

        public const string COMMANDID = "cancelcommand";

        public ClearCommand(Telegram telegram, UnitOfWork dbContext, Logger logger) : base(telegram, dbContext, logger)
        {
            CommandId = COMMANDID;
        }

        public override bool IsThisCommand(string message)
        {
            return message.Equals(COMMAND) || message.Equals(COMMANDID);
        }

        public override async Task WorkerAsync(Update update)
        {
            _logger.Info("ClearCommand - Start", GetUserId(update));

            ClearCommands(GetUserId(update));
            _telegram.SendMessageAsync(new SendMessageRequest
            {
                Text = "Chain had clear 😊",
                ChatId = update.Message.Chat.Id.ToString(),
                ReplyMarkup = new ReplyKeyboardHide() { IsHideKeyboard = true },
            }).ConfigureAwait(false);

            _logger.Info("ClearCommand - End", GetUserId(update));
        }
    }
}
