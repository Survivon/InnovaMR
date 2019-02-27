using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InnovaMRBot.Commands;
using InnovaMRBot.Helpers;
using InnovaMRBot.Models;
using InnovaMRBot.Repository;
using InnovaMRBot.Services;
using TelegramBotApi.Extension;
using TelegramBotApi.Models;
using TelegramBotApi.Models.Enum;
using TelegramBotApi.Models.Keyboard;
using TelegramBotApi.Telegram;
using TelegramBotApi.Telegram.Request;

namespace InnovaMRBot.SubCommand
{
    public class SprintUpdateActionSubCommand : BaseCommand
    {
        public const string SUB_COMMAND = "sprintupdateactionsubcommand";

        public SprintUpdateActionSubCommand(Telegram telegram, UnitOfWork dbContext, Logger logger) : base(telegram, dbContext, logger)
        {
            CommandId = SUB_COMMAND;
        }

        public override async Task WorkerAsync(Update update)
        {
            _logger.Info("SprintUpdateActionSubCommand - Start", GetUserId(update));

            UpdateCommand(GetUserId(update), CommandId, string.Empty);

            var responseMessage = new SendMessageRequest()
            {
                ChatId = update.Message.Chat.Id.ToString(),
                Text = "Please select sprint number",
                FormattingMessageType = FormattingMessageType.Markdown,
            };

            var sprints = _dbContext.Conversations.GetAll().FirstOrDefault(c => c.MRChat != null).MRChat.Sprints.Select(s => s.Number).OrderBy(c => c).ToList();

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

            _telegram.SendMessageAsync(responseMessage).ConfigureAwait(false);

            _logger.Info("SprintUpdateActionSubCommand - End", GetUserId(update));
        }

        public override async Task WorkOnAnswerAsync(Update update)
        {
            _logger.Info("SprintUpdateActionSubCommand - Start", GetUserId(update));

            UpdateCommand(GetUserId(update), CommandId, update.Message.Text);

            new SprintUpdateDateActionSubCommand(_telegram, _dbContext, _logger).WorkerAsync(update).ConfigureAwait(false);

            _logger.Info("SprintUpdateActionSubCommand - End", GetUserId(update));
        }
    }

    public class SprintUpdateDateActionSubCommand : BaseCommand
    {
        public const string SUB_COMMAND = "sprintupdatdateeactionsubcommand";

        public SprintUpdateDateActionSubCommand(Telegram telegram, UnitOfWork dbContext, Logger logger) : base(telegram, dbContext, logger)
        {
            CommandId = SUB_COMMAND;
        }

        public override async Task WorkerAsync(Update update)
        {
            _logger.Info("SprintUpdateDateActionSubCommand - Start", GetUserId(update));

            UpdateCommand(GetUserId(update), CommandId, string.Empty);

            var responseMessage = new SendMessageRequest()
            {
                ChatId = update.Message.Chat.Id.ToString(),
                Text = "Please input date from and to in format M/dd/yyyy M/dd/yyyy",
                FormattingMessageType = FormattingMessageType.Markdown,
                ReplyMarkup = new ReplyKeyboardHide() { IsHideKeyboard = true },
            };

            _telegram.SendMessageAsync(responseMessage).ConfigureAwait(false);

            _logger.Info("SprintUpdateDateActionSubCommand - End", GetUserId(update));
        }
        
        public override async Task WorkOnAnswerAsync(Update update)
        {
            _logger.Info("SprintUpdateDateActionSubCommand - Start", GetUserId(update));

            UpdateCommand(GetUserId(update), CommandId, update.Message.Text);

            var message = update.Message.Text;

            var components = message.Split(' ').ToList();

            var responseMessage = string.Empty;

            if (components.Count == 2)
            {
                try
                {
                    var startDate = components[0].ConvertToDate();
                    var endDate = components[1].ConvertToDate();

                    var commands = GetCommand(GetUserId(update));

                    var answer = commands.FirstOrDefault(c => c.Command.Equals(SprintUpdateActionSubCommand.SUB_COMMAND));

                    if (answer != null && int.TryParse(answer.Answer, out int number))
                    {
                        var conversations = _dbContext.Conversations.GetAll();
                        var conversation = conversations.FirstOrDefault(c => c.MRChat != null);

                        var sprints = conversation.MRChat.Sprints ?? new List<Sprint>();

                        var sprint = sprints.FirstOrDefault(s => s.Number == number);

                        if (sprint != null)
                        {
                            sprint.End = endDate;
                            sprint.Start = startDate;
                            responseMessage = "You successfuly update sprint info";
                        }

                        _dbContext.Conversations.Update(conversation);
                        ClearCommands(update.Message.Sender.Id.ToString());
                    }
                }
                catch (Exception e)
                {
                    responseMessage = "Incorrect date input try format M/d/yyyy";
                }
            }
            else
            {
                responseMessage = "Please enter two dates in format M/dd/yyyy";
            }
            
            var responseMessageRequest = new SendMessageRequest()
            {
                ChatId = update.Message.Chat.Id.ToString(),
                Text = responseMessage,
                FormattingMessageType = FormattingMessageType.Markdown,
                ReplyMarkup = new ReplyKeyboardHide() { IsHideKeyboard = true },
            };

            _telegram.SendMessageAsync(responseMessageRequest).ConfigureAwait(false);

            _logger.Info("SprintUpdateDateActionSubCommand - End", GetUserId(update));
        }
    }
}
