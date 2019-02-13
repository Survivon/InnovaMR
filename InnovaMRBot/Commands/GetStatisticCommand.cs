using InnovaMRBot.Helpers;
using InnovaMRBot.Models;
using InnovaMRBot.Repository;
using InnovaMRBot.SubCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TelegramBotApi.Extension;
using TelegramBotApi.Models;
using TelegramBotApi.Models.Enum;
using TelegramBotApi.Models.Keyboard;
using TelegramBotApi.Telegram;
using TelegramBotApi.Telegram.Request;

namespace InnovaMRBot.Commands
{
    public class GetStatisticCommand : BaseCommand
    {
        public const string COMMAND_ID = "statisticmaincommand";

        private const string COMMAND = Glossary.Stat.COMMAND;

        private readonly Dictionary<string, BaseCommand> _subCommand = new Dictionary<string, BaseCommand>();

        public GetStatisticCommand(Telegram telegram, UnitOfWork dbContext)
            : base(telegram, dbContext)
        {
            CommandId = COMMAND_ID;

            _subCommand = new Dictionary<string, BaseCommand>()
            {
                { Glossary.Stat.ALL, new GetStatisticAllActionSubCommand(telegram, _dbContext) },
                { Glossary.Stat.DATE, new GetStatisticDateActionSubCommand(telegram, _dbContext) },
                { Glossary.Stat.SPRINT, new GetStatisticSprintActionSubCommand(telegram, _dbContext) },
            };
        }

        public override bool IsThisCommand(string message)
        {
            return message.StartsWith(COMMAND) || message.StartsWith(COMMAND_ID);
        }

        public override async Task WorkerAsync(Update update)
        {
            var userId = update.Message.Sender.Id.ToString();
            var message = update.Message.Text;

            if (StatisticGlossary.StatisticCommand.ContainsKey(message.Replace(COMMAND, string.Empty)))
            {
                UpdateCommand(userId, $"{CommandId}{message.Replace(COMMAND, string.Empty)}", string.Empty);

                var requestWithCommandMessage = new SendMessageRequest
                {
                    ChatId = update.Message.Chat.Id.ToString(),
                    Text = "Please select action for get stat",
                    FormattingMessageType = FormattingMessageType.Markdown,
                    ReplyMarkup = new ReplyKeyboardMarkup()
                    {
                        IsHideKeyboardAfterClick = true,
                        Keyboard = new List<List<KeyboardButton>>()
                        {
                            new List<KeyboardButton>(_subCommand.Keys.Select(k => new KeyboardButton { Text = k })),
                        },
                    },
                };

                _telegram.SendMessageAsync(requestWithCommandMessage).ConfigureAwait(false);
            }
            else
            {
                message = message.Replace(COMMAND, string.Empty).Trim();
                var components = message.Split(' ');
                var command = components[0];
                var startDate = default(DateTimeOffset);
                var endDate = default(DateTimeOffset);
                var conversations = _dbContext.Conversations.GetAll();
                var conversation = conversations.FirstOrDefault(c => c.MRChat != null);

                if (components.Length > 2 && components[1].Equals("sprint"))
                {
                    var number = Convert.ToInt32(components[2]);

                    var sprint = conversation.MRChat.Sprints.FirstOrDefault(s => s.Number == number);
                    if (sprint != null)
                    {
                        startDate = new DateTimeOffset(sprint.Start);
                        endDate = new DateTimeOffset(sprint.End);
                    }
                }
                else
                {
                    if (components.Length > 1)
                    {
                        startDate = new DateTimeOffset(Convert.ToDateTime(components[1]));
                    }

                    if (components.Length > 2)
                    {
                        endDate = new DateTimeOffset(Convert.ToDateTime(components[2]));
                    }
                }

                SaveIfNeedUser(update.Message.Sender);

                var result = string.Empty;
                var users = _dbContext.Users.GetAll().ToList();

                var currentUser = users.FirstOrDefault(u => u.UserId == GetUserId(update));

                if (conversation != null)
                {
                    if (StatisticGlossary.StatisticCommand.ContainsKey(command))
                    {
                        result = StatisticGlossary.StatisticCommand[command](conversation.ListOfMerge.ToList(), users, currentUser, startDate, endDate);
                    }

                    var responseMessageForUser = new SendDocumentRequest
                    {
                        ChatId = update.Message.Chat.Id.ToString(),
                        Document = string.Empty,
                    };

                    _telegram.SendDocumentAsync(responseMessageForUser, result).ConfigureAwait(false);
                }
            }
        }

        public override async Task WorkOnAnswerAsync(Update update)
        {
            UpdateCommand(GetUserId(update), GetPrevCommand(GetUserId(update)), update.Message.Text);

            var answer = update.Message.Text;

            if (_subCommand.ContainsKey(answer))
            {
                _subCommand[answer].WorkerAsync(update).ConfigureAwait(false);
            }
        }

        private string GetPrevCommand(string userId)
        {
            var user = _dbContext.Users.GetAll().FirstOrDefault(u => u.UserId.Equals(userId));

            var lastAnswer = user.Commands.LastOrDefault();

            return lastAnswer?.Command ?? string.Empty;
        }
    }

    public class GetStatisticAllActionSubCommand : BaseCommand
    {
        public const string SUB_COMMAND = "getstatallactionsubcommand";

        public GetStatisticAllActionSubCommand(Telegram telegram, UnitOfWork dbContext) : base(telegram, dbContext)
        {
            CommandId = SUB_COMMAND;
        }

        public override async Task WorkerAsync(Update update)
        {
            var message = update.Message.Text;

            if (message.Equals(Glossary.Stat.ALL))
            {
                var user = _dbContext.Users.GetAll().FirstOrDefault(u => u.UserId.Equals(GetUserId(update)));

                var lastAnswer = user.Commands.LastOrDefault();

                var stat = lastAnswer.Command.Replace(GetStatisticCommand.COMMAND_ID, string.Empty);

                if (StatisticGlossary.StatisticCommand.ContainsKey(stat))
                {
                    var conversations = _dbContext.Conversations.GetAll();
                    var conversation = conversations.FirstOrDefault(c => c.MRChat != null);

                    SaveIfNeedUser(update.Message.Sender);

                    var result = string.Empty;
                    var users = _dbContext.Users.GetAll().ToList();
                    var currentUser = users.FirstOrDefault(u => u.UserId == GetUserId(update));

                    result = StatisticGlossary.StatisticCommand[stat](conversation.ListOfMerge.ToList(), users, currentUser, default(DateTimeOffset), default(DateTimeOffset));

                    var responseMessageForUser = new SendDocumentRequest
                    {
                        ChatId = update.Message.Chat.Id.ToString(),
                        Document = string.Empty,
                        ReplyMarkup = new ReplyKeyboardHide() { IsHideKeyboard = true },
                    };

                    _telegram.SendDocumentAsync(responseMessageForUser, result).ConfigureAwait(false);

                    ClearCommands(GetUserId(update));
                }
            }
        }
    }

    public class GetStatisticSprintActionSubCommand : BaseCommand
    {
        public const string SUB_COMMAND = "getstatsprintactionsubcommand";

        public GetStatisticSprintActionSubCommand(Telegram telegram, UnitOfWork dbContext) : base(telegram, dbContext)
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
        }

        public override async Task WorkOnAnswerAsync(Update update)
        {
            UpdateCommand(GetUserId(update), CommandId, update.Message.Text);

            if (int.TryParse(update.Message.Text, out int number))
            {
                var user = _dbContext.Users.GetAll().FirstOrDefault(u => u.UserId.Equals(GetUserId(update)));

                var lastAnswer = user.Commands.FirstOrDefault();

                var stat = lastAnswer.Command.Replace(GetStatisticCommand.COMMAND_ID, string.Empty);

                if (StatisticGlossary.StatisticCommand.ContainsKey(stat))
                {
                    var conversations = _dbContext.Conversations.GetAll();
                    var conversation = conversations.FirstOrDefault(c => c.MRChat != null);

                    SaveIfNeedUser(update.Message.Sender);

                    var result = string.Empty;
                    var users = _dbContext.Users.GetAll().ToList();
                    var currentUser = users.FirstOrDefault(u => u.UserId == GetUserId(update));

                    var startDate = default(DateTimeOffset);
                    var endDate = default(DateTimeOffset);

                    var sprint = conversation.MRChat.Sprints.FirstOrDefault(s => s.Number == number);
                    if (sprint != null)
                    {
                        startDate = new DateTimeOffset(sprint.Start);
                        endDate = new DateTimeOffset(sprint.End);
                    }

                    result = StatisticGlossary.StatisticCommand[stat](conversation.ListOfMerge.ToList(), users, currentUser,
                        startDate, endDate);

                    var responseMessageForUser = new SendDocumentRequest
                    {
                        ChatId = update.Message.Chat.Id.ToString(),
                        Document = string.Empty,
                        ReplyMarkup = new ReplyKeyboardHide() { IsHideKeyboard = true },
                    };

                    _telegram.SendDocumentAsync(responseMessageForUser, result).ConfigureAwait(false);

                    ClearCommands(GetUserId(update));
                }
            }
        }
    }

    public class GetStatisticDateActionSubCommand : BaseCommand
    {
        public const string SUB_COMMAND = "getstatdateactionsubcommand";

        public GetStatisticDateActionSubCommand(Telegram telegram, UnitOfWork dbContext) : base(telegram, dbContext)
        {
            CommandId = SUB_COMMAND;
        }

        public override async Task WorkerAsync(Update update)
        {
            UpdateCommand(GetUserId(update), CommandId, string.Empty);

            var responseMessage = new SendMessageRequest()
            {
                ChatId = update.Message.Chat.Id.ToString(),
                Text = "Please input date from and to in format M/dd/yyyy M/dd/yyyy",
                FormattingMessageType = FormattingMessageType.Markdown,
            };

            _telegram.SendMessageAsync(responseMessage).ConfigureAwait(false);
        }

        public override async Task WorkOnAnswerAsync(Update update)
        {
            var message = update.Message.Text;

            UpdateCommand(GetUserId(update), CommandId, message);
            var components = message.Split(' ').ToList();
            var responseMessage = string.Empty;
            var result = string.Empty;

            if (components.Count == 2)
            {
                var user = _dbContext.Users.GetAll().FirstOrDefault(u => u.UserId.Equals(GetUserId(update)));

                var lastAnswer = user.Commands.FirstOrDefault();

                var stat = lastAnswer.Command.Replace(GetStatisticCommand.COMMAND_ID, string.Empty);

                if (StatisticGlossary.StatisticCommand.ContainsKey(stat))
                {
                    var conversations = _dbContext.Conversations.GetAll();
                    var conversation = conversations.FirstOrDefault(c => c.MRChat != null);

                    SaveIfNeedUser(update.Message.Sender);

                    var users = _dbContext.Users.GetAll().ToList();
                    var currentUser = users.FirstOrDefault(u => u.UserId == GetUserId(update));

                    try
                    {
                        var startDate = components[0].ConvertToDate();
                        var endDate = components[1].ConvertToDate();

                        result = StatisticGlossary.StatisticCommand[stat](conversation.ListOfMerge.ToList(), users, currentUser,
                            startDate, endDate);

                        ClearCommands(GetUserId(update));
                    }
                    catch (Exception e)
                    {
                        responseMessage = "Please enter dates in format M/dd/yyyy";
                    }
                }
            }
            else
            {
                responseMessage = "Please enter two dates in format M/dd/yyyy";
            }

            if (string.IsNullOrEmpty(responseMessage))
            {
                var responseMessageForUser = new SendDocumentRequest
                {
                    ChatId = update.Message.Chat.Id.ToString(),
                    Document = string.Empty,
                    ReplyMarkup = new ReplyKeyboardHide() { IsHideKeyboard = true },
                };

                _telegram.SendDocumentAsync(responseMessageForUser, result).ConfigureAwait(false);
            }
            else
            {
                _telegram.SendMessageAsync(new SendMessageRequest()
                {
                    ChatId = update.Message.Chat.Id.ToString(),
                    Text = responseMessage,
                    FormattingMessageType = FormattingMessageType.Markdown,
                    ReplyMarkup = new ReplyKeyboardHide() { IsHideKeyboard = true },
                }).ConfigureAwait(false);
            }
        }
    }
}
