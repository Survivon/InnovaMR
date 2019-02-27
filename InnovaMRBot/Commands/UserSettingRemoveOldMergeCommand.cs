using System;
using System.Collections.Generic;
using System.Linq;
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
    public class UserSettingRemoveOldMergeCommand : BaseCommand
    {
        private const string COMMAND = "/old_merge_remove";

        private const string COMMANDID = "oldmergeremove";

        public UserSettingRemoveOldMergeCommand(Telegram telegram, UnitOfWork dbContext, Logger logger) : base(telegram, dbContext, logger)
        {
            CommandId = COMMANDID;
        }

        public override bool IsThisCommand(string message)
        {
            return message.Equals(COMMAND) || message.Equals(COMMANDID);
        }

        public override async Task WorkerAsync(Update update)
        {
            _logger.Info("UserSettingRemoveOldMergeCommand - Start", GetUserId(update));

            UpdateCommand(GetUserId(update), COMMANDID, string.Empty);

            _telegram.SendMessageAsync(new SendMessageRequest
            {
                Text = "Do you want to remove old merges, which has new version?",
                ChatId = update.Message.Chat.Id.ToString(),
                ReplyMarkup = new ReplyKeyboardMarkup()
                {
                    Keyboard = new List<List<KeyboardButton>>()
                    {
                        new List<KeyboardButton>()
                        {
                            new KeyboardButton()
                            {
                                Text = "Yes",
                            },
                            new KeyboardButton()
                            {
                                Text = "No",
                            },
                        },
                    },
                },
            }).ConfigureAwait(false);

            _logger.Info("UserSettingRemoveOldMergeCommand - End", GetUserId(update));
        }

        public override async Task WorkOnAnswerAsync(Update update)
        {
            _logger.Info("UserSettingRemoveOldMergeCommand - Start", GetUserId(update));

            var message = update.Message.Text;
            UpdateCommand(GetUserId(update), COMMANDID, message);

            if (message.Equals("Yes", StringComparison.InvariantCultureIgnoreCase) ||
                message.Equals("No", StringComparison.InvariantCultureIgnoreCase))
            {
                var user = _dbContext.Users.GetAll().FirstOrDefault(u => u.UserId.Equals(GetUserId(update)));
                if (message.Equals("Yes", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!user.CanRemoveOldMr)
                    {
                        user.CanRemoveOldMr = true;
                    }
                }
                else
                {
                    if (user.CanRemoveOldMr)
                    {
                        user.CanRemoveOldMr = false;
                    }
                }

                _telegram.SendMessageAsync(new SendMessageRequest
                {
                    Text = "Done 😊",
                    ChatId = update.Message.Chat.Id.ToString(),
                    ReplyMarkup = new ReplyKeyboardHide() { IsHideKeyboard = true },
                }).ConfigureAwait(false);

                _dbContext.Users.Update(user);
                _dbContext.Save();

                ClearCommands(GetUserId(update));
            }
            else
            {
                _telegram.SendMessageAsync(new SendMessageRequest
                {
                    Text = "Need to choose Yes or No",
                    ChatId = update.Message.Chat.Id.ToString(),
                }).ConfigureAwait(false);
            }

            _logger.Info("UserSettingRemoveOldMergeCommand - End", GetUserId(update));
        }
    }
}
