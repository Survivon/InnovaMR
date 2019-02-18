using System.Threading.Tasks;
using InnovaMRBot.Helpers;
using InnovaMRBot.Repository;
using InnovaMRBot.Services;
using TelegramBotApi.Extension;
using TelegramBotApi.Models;
using TelegramBotApi.Telegram;
using TelegramBotApi.Telegram.Request;
using User = InnovaMRBot.Models.User;

namespace InnovaMRBot.Commands
{
    public class StartCommand : BaseCommand
    {
        private const string COMMAND = "/start";

        public StartCommand(Telegram telegram, UnitOfWork dbContext, Logger logger) : base(telegram, dbContext, logger)
        {
            CommandId = "startcommand";
        }

        protected override EqualType GetType()
        {
            return EqualType.Equal;
        }

        protected override string GetCommandString()
        {
            return COMMAND;
        }

        public override async Task WorkerAsync(Update update)
        {
            var savedUser = new User
            {
                Name = update.Message.Sender.GetUserFullName(),
                UserId = update.Message.Sender.Id.ToString(),
            };

            AddOrUpdateUser(savedUser, false);

            // get start message
            _telegram.SendMessageAsync(new SendMessageRequest
            {
                ChatId = update.Message.Chat.Id.ToString(),
                Text = $"Hi, {savedUser.Name}! I'm Bot for help to work with MR for Innova 😊 If you have some question please send me /help or visit http://innovamrbot.azurewebsites.net/",
            }).ConfigureAwait(false);
        }
    }
}
