using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InnovaMRBot.Helpers;
using InnovaMRBot.Repository;
using TelegramBotApi.Extension;
using TelegramBotApi.Models;
using TelegramBotApi.Models.Keyboard;
using TelegramBotApi.Telegram;
using TelegramBotApi.Telegram.Request;

namespace InnovaMRBot.Commands
{
    public class UserSettingTimeZoneSetupCommand : BaseCommand
    {
        private const string COMMAND = "/setup_timezone";

        private const string COMMANDID = "setuptimezonecommand";

        public UserSettingTimeZoneSetupCommand(Telegram telegram, UnitOfWork dbContext) : base(telegram, dbContext)
        {
            CommandId = COMMANDID;
        }

        public override bool IsThisCommand(string message)
        {
            return message.Equals(COMMANDID) || message.Equals(COMMAND);
        }

        public override async Task WorkerAsync(Update update)
        {
            UpdateCommand(GetUserId(update), COMMANDID, string.Empty);

            var keyboard = new List<List<KeyboardButton>>();

            for (int i = 0; i < TimeZoneHelper.Time.Count; i++)
            {
                if (i % 3 == 0)
                {
                    keyboard.Add(new List<KeyboardButton>());
                }

                keyboard.LastOrDefault().Add(new KeyboardButton()
                {
                    Text = TimeZoneHelper.Time.ElementAt(i).Key,
                });
            }

            _telegram.SendMessageAsync(new SendMessageRequest
            {
                Text = "Please, select your country",
                ChatId = update.Message.Chat.Id.ToString(),
                ReplyMarkup = new ReplyKeyboardMarkup()
                {
                    Keyboard = keyboard,
                },
            }).ConfigureAwait(false);
        }

        public override async Task WorkOnAnswerAsync(Update update)
        {
            var message = update.Message.Text;
            UpdateCommand(GetUserId(update), COMMANDID, string.Empty);

            if (TimeZoneHelper.Time.ContainsKey(message))
            {
                var timeDiff = TimeZoneHelper.Time[message];

                var user = _dbContext.Users.GetAll().FirstOrDefault(u => u.UserId.Equals(GetUserId(update)));

                if (user.TimeDiff != timeDiff)
                {
                    user.TimeDiff = timeDiff;
                    _dbContext.Users.Update(user);
                    _dbContext.Save();
                }

                ClearCommands(GetUserId(update));

                _telegram.SendMessageAsync(new SendMessageRequest
                {
                    Text = "Done 😊",
                    ChatId = update.Message.Chat.Id.ToString(),
                    ReplyMarkup = new ReplyKeyboardHide() { IsHideKeyboard = true },
                }).ConfigureAwait(false);
            }
            else
            {
                _telegram.SendMessageAsync(new SendMessageRequest
                {
                    Text = "Please select some value from collection",
                    ChatId = update.Message.Chat.Id.ToString(),
                }).ConfigureAwait(false);
            }
        }
    }
}
