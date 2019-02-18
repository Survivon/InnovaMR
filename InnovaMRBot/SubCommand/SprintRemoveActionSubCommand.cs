using InnovaMRBot.Repository;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InnovaMRBot.Commands;
using InnovaMRBot.Models;
using InnovaMRBot.Services;
using TelegramBotApi.Extension;
using TelegramBotApi.Models;
using TelegramBotApi.Models.Enum;
using TelegramBotApi.Models.Keyboard;
using TelegramBotApi.Telegram;
using TelegramBotApi.Telegram.Request;

namespace InnovaMRBot.SubCommand
{
    public class SprintRemoveActionSubCommand : BaseCommand
    {
        public const string SUB_COMMAND = "sprintremoveactionsubcommand";

        public SprintRemoveActionSubCommand(Telegram telegram, UnitOfWork dbContext, Logger logger) : base(telegram, dbContext, logger)
        {
            CommandId = SUB_COMMAND;
        }

        public override async Task WorkerAsync(Update update)
        {
            UpdateCommand(GetUserId(update), CommandId, string.Empty);

            var responseMessage = new SendMessageRequest()
            {
                ChatId = update.Message.Chat.Id.ToString(),
                Text = "Please select sprint number",
                FormattingMessageType = FormattingMessageType.Markdown,
            };
            
            var sprints = _dbContext.Conversations.GetAll().FirstOrDefault(c => c.MRChat != null).MRChat.Sprints.Select(s => s.Number).OrderBy(c => c).ToList();

            if (sprints.Any())
            {
                var keyboardArray = new ReplyKeyboardMarkup()
                {
                    Keyboard = new List<List<KeyboardButton>>(),
                };

                for (var i = 0; i < sprints.Count; i++)
                {
                    if (i % 3 == 0)
                    {
                        keyboardArray.Keyboard.Add(new List<KeyboardButton>());
                    }

                    keyboardArray.Keyboard.LastOrDefault().Add(new KeyboardButton()
                    {
                        Text = sprints[i].ToString(),
                    });
                }

                responseMessage.ReplyMarkup = keyboardArray;
            }
            else
            {
                responseMessage.Text = "You don't have any sprints 😔";
                responseMessage.ReplyMarkup = new ReplyKeyboardHide() { IsHideKeyboard = true };
                ClearCommands(GetUserId(update));
            }

            _telegram.SendMessageAsync(responseMessage).ConfigureAwait(false);
        }

        public override async Task WorkOnAnswerAsync(Update update)
        {
            UpdateCommand(GetUserId(update), CommandId, update.Message.Text);

            var conversations = _dbContext.Conversations.GetAll();
            var conversation = conversations.FirstOrDefault(c => c.MRChat != null);

            var sprints = conversation.MRChat.Sprints ?? new List<Sprint>();

            var responseMessage = string.Empty;

            var request = new SendMessageRequest()
            {
                ChatId = update.Message.Chat.Id.ToString(),
                FormattingMessageType = FormattingMessageType.Markdown,
            };

            if (sprints.Any() && int.TryParse(update.Message.Text, out int number))
            {
                var sprint = sprints.FirstOrDefault(s => s.Number == number);

                if (sprint != null)
                {
                    sprints.Remove(sprint);
                    _dbContext.Conversations.Update(conversation);
                    ClearCommands(GetUserId(update));
                    responseMessage = $"Sprint {number} is removed";
                    request.ReplyMarkup = new ReplyKeyboardHide() { IsHideKeyboard = true };
                }
            }
            else
            {
                responseMessage = "You don't have any sprints";
                ClearCommands(GetUserId(update));
            }

            request.Text = responseMessage;
            
            _telegram.SendMessageAsync(request).ConfigureAwait(false);
        }
    }
}
