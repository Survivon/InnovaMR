using InnovaMRBot.Models;
using InnovaMRBot.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InnovaMRBot.Commands;
using InnovaMRBot.Helpers;
using InnovaMRBot.Services;
using TelegramBotApi.Extension;
using TelegramBotApi.Models;
using TelegramBotApi.Models.Enum;
using TelegramBotApi.Models.Keyboard;
using TelegramBotApi.Telegram;
using TelegramBotApi.Telegram.Request;

namespace InnovaMRBot.SubCommand
{
    public class SprintAddActionSubCommand : BaseCommand
    {
        public const string SUB_COMMAND = "sprintaddactionsubcommand";

        public SprintAddActionSubCommand(Telegram telegram, UnitOfWork dbContext, Logger logger) : base(telegram, dbContext, logger)
        {
            CommandId = SUB_COMMAND;
        }

        public override async Task WorkerAsync(Update update)
        {
            _logger.Info("SprintAddActionSubCommand - Start", GetUserId(update));

            UpdateCommand(GetUserId(update), CommandId, string.Empty);

            var responseMessage = new SendMessageRequest()
            {
                ChatId = update.Message.Chat.Id.ToString(),
                Text = "Please input new sprint number",
                FormattingMessageType = FormattingMessageType.Markdown,
                ReplyMarkup = new ReplyKeyboardHide() { IsHideKeyboard = true },
            };

            _telegram.SendMessageAsync(responseMessage).ConfigureAwait(false);

            _logger.Info("SprintAddActionSubCommand - End", GetUserId(update));
        }

        public override async Task WorkOnAnswerAsync(Update update)
        {
            _logger.Info("SprintAddActionSubCommand - Start", GetUserId(update));

            var message = update.Message.Text;

            UpdateCommand(GetUserId(update), CommandId, message);

            if (int.TryParse(message, out int number))
            {
                var conversations = _dbContext.Conversations.GetAll();
                var conversation = conversations.FirstOrDefault(c => c.MRChat != null);

                var isSprintExist = conversation.MRChat.Sprints.Any(s => s.Number.Equals(number));

                if (isSprintExist)
                {
                    var responseMessage = new SendMessageRequest()
                    {
                        ChatId = update.Message.Chat.Id.ToString(),
                        Text = $"Sprint number *{number}* is already exist 😔 Try enter other sprint number",
                        FormattingMessageType = FormattingMessageType.Markdown,
                        ReplyMarkup = new ReplyKeyboardHide() { IsHideKeyboard = true },
                    };

                    _telegram.SendMessageAsync(responseMessage).ConfigureAwait(false);
                }
                else
                {
                    new SprintAddDateActionSubCommand(_telegram, _dbContext, _logger).WorkerAsync(update).ConfigureAwait(false);
                }
            }
            else
            {
                var responseMessage = new SendMessageRequest()
                {
                    ChatId = update.Message.Chat.Id.ToString(),
                    Text = "Please input number",
                    FormattingMessageType = FormattingMessageType.Markdown,
                    ReplyMarkup = new ReplyKeyboardHide() { IsHideKeyboard = true },
                };

                _telegram.SendMessageAsync(responseMessage).ConfigureAwait(false);
            }

            _logger.Info("SprintAddActionSubCommand - End", GetUserId(update));
        }
    }

    public class SprintAddDateActionSubCommand : BaseCommand
    {
        public const string SUB_COMMAND = "sprintadddateactionsubcommand";

        public SprintAddDateActionSubCommand(Telegram telegram, UnitOfWork dbContext, Logger logger) : base(telegram, dbContext, logger)
        {
            CommandId = SUB_COMMAND;
        }

        public override async Task WorkerAsync(Update update)
        {
            _logger.Info("SprintAddDateActionSubCommand - Start", GetUserId(update));

            UpdateCommand(GetUserId(update), CommandId, string.Empty);

            var responseMessage = new SendMessageRequest()
            {
                ChatId = update.Message.Chat.Id.ToString(),
                Text = "Please input date from and to in format M/dd/yyyy M/dd/yyyy",
                FormattingMessageType = FormattingMessageType.Markdown,
                ReplyMarkup = new ReplyKeyboardHide() { IsHideKeyboard = true },
            };

            _telegram.SendMessageAsync(responseMessage).ConfigureAwait(false);

            _logger.Info("SprintAddDateActionSubCommand - End", GetUserId(update));
        }

        public override async Task WorkOnAnswerAsync(Update update)
        {
            _logger.Info("SprintAddDateActionSubCommand - Start", GetUserId(update));

            var message = update.Message.Text;

            UpdateCommand(GetUserId(update), CommandId, message);

            var components = message.Split(' ').ToList();

            var responseMessage = string.Empty;

            if (components.Count == 2)
            {
                try
                {
                    var startDate = components[0].ConvertToDate();
                    var endDate = components[1].ConvertToDate();

                    var commands = GetCommand(GetUserId(update));

                    var answer = commands.FirstOrDefault(c => c.Command.Equals(SprintAddActionSubCommand.SUB_COMMAND));

                    if (answer != null && int.TryParse(answer.Answer, out int number))
                    {
                        var conversations = _dbContext.Conversations.GetAll();
                        var conversation = conversations.FirstOrDefault(c => c.MRChat != null);

                        var sprints = conversation.MRChat.Sprints ?? new List<Sprint>();

                        sprints.Add(new Sprint()
                        {
                            Number = number,
                            Start = startDate,
                            End = endDate,
                        });

                        responseMessage = "Sprint save!";

                        _dbContext.Conversations.Update(conversation);
                        ClearCommands(update.Message.Sender.Id.ToString());
                    }
                }
                catch (Exception e)
                {
                    responseMessage = "Please enter dates in format M/dd/yyyy";
                    _logger.Error(e.Message, GetUserId(update));
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

            _logger.Info("SprintAddDateActionSubCommand - End", GetUserId(update));
        }
    }
}
