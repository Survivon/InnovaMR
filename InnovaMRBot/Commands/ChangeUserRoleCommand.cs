using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InnovaMRBot.Models.Enum;
using InnovaMRBot.Repository;
using TelegramBotApi.Extension;
using TelegramBotApi.Models;
using TelegramBotApi.Models.Keyboard;
using TelegramBotApi.Telegram;
using TelegramBotApi.Telegram.Request;

namespace InnovaMRBot.Commands
{
    public class ChangeUserRoleCommand : BaseCommand
    {
        private const string COMMAND = "/change_role";

        private const string COMMANDID = "changerole";

        public static readonly Dictionary<UserRole, string> UserRoleMapping = new Dictionary<UserRole, string>()
        {
            { UserRole.Dev, "Dev" },
            { UserRole.PM, "PM" },
            { UserRole.QA, "QA" },
        };

        public ChangeUserRoleCommand(Telegram telegram, UnitOfWork dbContext) : base(telegram, dbContext)
        {
            CommandId = COMMANDID;
        }

        public override bool IsThisCommand(string message)
        {
            return message.Equals(COMMAND) || message.Equals(COMMANDID);
        }

        public override async Task WorkerAsync(Update update)
        {
            UpdateCommand(GetUserId(update), COMMANDID, string.Empty);

            var keyBoard = new List<KeyboardButton>();

            foreach (var role in UserRoleMapping)
            {
                keyBoard.Add(new KeyboardButton()
                {
                    Text = role.Value,
                });
            }

            _telegram.SendMessageAsync(new SendMessageRequest
            {
                Text = "Please select your role",
                ChatId = update.Message.Chat.Id.ToString(),
                ReplyMarkup = new ReplyKeyboardMarkup()
                {
                    Keyboard = new List<List<KeyboardButton>>()
                    {
                        keyBoard,
                    },
                },
            }).ConfigureAwait(false);
        }

        public override async Task WorkOnAnswerAsync(Update update)
        {
            var message = update.Message.Text;
            UpdateCommand(GetUserId(update), COMMANDID, message);

            if (UserRoleMapping.ContainsValue(message))
            {
                var role = UserRoleMapping.FirstOrDefault(c => c.Value.Equals(message));

                var user = _dbContext.Users.GetAll().FirstOrDefault(c => c.UserId.Equals(GetUserId(update)));

                user.Role = role.Key;

                _telegram.SendMessageAsync(new SendMessageRequest
                {
                    Text = "Done 😊",
                    ChatId = update.Message.Chat.Id.ToString(),
                }).ConfigureAwait(false);

                ClearCommands(GetUserId(update));

                _dbContext.Users.Update(user);
                _dbContext.Save();
            }
            else
            {
                _telegram.SendMessageAsync(new SendMessageRequest
                {
                    Text = "Incorrect role! Please, select your role (Dev, QA, PM)",
                    ChatId = update.Message.Chat.Id.ToString(),
                }).ConfigureAwait(false);
            }
        }
    }
}
