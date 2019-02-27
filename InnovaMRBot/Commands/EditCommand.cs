using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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

namespace InnovaMRBot.Commands
{
    public class EditCommand : BaseCommand
    {
        private const int MAX_DAYS_MR_FOR_CHANGE = 1;

        public const string COMMAND = "/edit";

        public const string COMMANDID = "editcommand";

        public EditCommand(Telegram telegram, UnitOfWork dbContext, Logger logger) : base(telegram, dbContext, logger)
        {
            CommandId = COMMANDID;
        }

        public override bool IsThisCommand(string message)
        {
            return message.StartsWith(COMMAND) || message.Equals(COMMANDID);
        }

        public override async Task WorkerAsync(Update update)
        {
            _logger.Info("EditCommand - Start", GetUserId(update));

            UpdateCommand(GetUserId(update), COMMANDID, string.Empty);

            var message = update.Message.Text;

            var conversations = _dbContext.Conversations.GetAll();
            var conversation = conversations.FirstOrDefault(c => c.MRChat != null);

            if (message.Equals(COMMAND))
            {
                var responseMessage = new SendMessageRequest()
                {
                    ChatId = update.Message.Chat.Id.ToString(),
                    Text = "Please select MR number",
                    FormattingMessageType = FormattingMessageType.Markdown,
                };

                var mrs = conversation.ListOfMerge.Where(m =>
                    m.PublishDate > new DateTimeOffset(DateTime.UtcNow.Subtract(TimeSpan.FromDays(MAX_DAYS_MR_FOR_CHANGE)))
                    && CanMrBeChanged(m)
                    && m.OwnerId.Equals(GetUserId(update))).ToList();

                var keyboardArray = new ReplyKeyboardMarkup()
                {
                    Keyboard = new List<List<KeyboardButton>>(),
                };

                for (var i = 0; i < mrs.Count; i++)
                {
                    if (i % 3 == 0)
                    {
                        keyboardArray.Keyboard.Add(new List<KeyboardButton>());
                    }

                    keyboardArray.Keyboard.LastOrDefault().Add(new KeyboardButton()
                    {
                        Text = new Regex(TICKET_NUMBER_PATTERN).Match(mrs[i].MrUrl).Value,
                    });
                }

                responseMessage.ReplyMarkup = keyboardArray;

                _telegram.SendMessageAsync(responseMessage).ConfigureAwait(false);
            }
            else if (int.TryParse(message.Replace(COMMAND, string.Empty).Trim(), out int ticketNumber))
            {
                var merge = conversation.ListOfMerge.FirstOrDefault(m =>
                    Regex.Match(m.MrUrl, TICKET_NUMBER_PATTERN).Value.Equals(ticketNumber.ToString()));

                var canMrChanged = CanMrBeChanged(merge);

                if (canMrChanged)
                {
                    UpdateCommand(GetUserId(update), CommandId, ticketNumber.ToString());
                    UpdateCommand(GetUserId(update), EditMergeNumberActionSubCommand.COMMANDID, string.Empty);

                    var responseMessage = new SendMessageRequest()
                    {
                        ChatId = update.Message.Chat.Id.ToString(),
                        Text = $"Please input new description for MR {ticketNumber}",
                        FormattingMessageType = FormattingMessageType.Markdown,
                        ReplyMarkup = new ReplyKeyboardHide() { IsHideKeyboard = true },
                    };

                    _telegram.SendMessageAsync(responseMessage).ConfigureAwait(false);
                }
                else
                {
                    var responseMessage = new SendMessageRequest()
                    {
                        ChatId = update.Message.Chat.Id.ToString(),
                        Text = "You couldn't change description for current MR, please choose MR from collection",
                        FormattingMessageType = FormattingMessageType.Markdown,
                    };

                    var mrs = conversation.ListOfMerge.Where(m =>
                        m.PublishDate > new DateTimeOffset(DateTime.UtcNow.Subtract(TimeSpan.FromDays(MAX_DAYS_MR_FOR_CHANGE)))
                        && CanMrBeChanged(m)
                        && m.OwnerId.Equals(GetUserId(update))).ToList();

                    var keyboardArray = new ReplyKeyboardMarkup()
                    {
                        Keyboard = new List<List<KeyboardButton>>(),
                    };

                    for (var i = 0; i < mrs.Count; i++)
                    {
                        if (i % 3 == 0)
                        {
                            keyboardArray.Keyboard.Add(new List<KeyboardButton>());
                        }

                        keyboardArray.Keyboard.LastOrDefault().Add(new KeyboardButton()
                        {
                            Text = new Regex(TICKET_NUMBER_PATTERN).Match(mrs[i].MrUrl).Value,
                        });
                    }

                    responseMessage.ReplyMarkup = keyboardArray;

                    _telegram.SendMessageAsync(responseMessage).ConfigureAwait(false);
                }
            }

            _logger.Info("EditCommand - End", GetUserId(update));
        }

        public override async Task WorkOnAnswerAsync(Update update)
        {
            _logger.Info("EditCommand - Start", GetUserId(update));

            UpdateCommand(GetUserId(update), CommandId, update.Message.Text);

            new EditMergeNumberActionSubCommand(_telegram, _dbContext, _logger).WorkerAsync(update).ConfigureAwait(false);

            _logger.Info("EditCommand - Start", GetUserId(update));
        }

        public static bool CanMrBeChanged(MergeSetting merge)
        {
            var result = false;

            if (merge.VersionedSetting.Any())
            {
                var lastVersion = merge.VersionedSetting.FirstOrDefault(c =>
                    c.PublishDate == merge.VersionedSetting.Max(v => v.PublishDate));

                result = !lastVersion.IsHadAlreadyChange;
            }
            else
            {
                result = !merge.IsHadAlreadyChange;
            }

            return result;
        }
    }

    public class EditMergeNumberActionSubCommand : BaseCommand
    {
        public const string COMMANDID = "editcommandnumberaction";

        public EditMergeNumberActionSubCommand(Telegram telegram, UnitOfWork dbContext, Logger logger) : base(telegram, dbContext, logger)
        {
            CommandId = COMMANDID;
        }

        public override async Task WorkerAsync(Update update)
        {
            _logger.Info("EditMergeNumberActionSubCommand - Start", GetUserId(update));

            var message = update.Message.Text;

            var requestMessage = string.Empty;

            var responseMessage = new SendMessageRequest()
            {
                ChatId = update.Message.Chat.Id.ToString(),
                FormattingMessageType = FormattingMessageType.Markdown,
            };
            
            if (int.TryParse(message, out int ticketNumber))
            {
                var conversations = _dbContext.Conversations.GetAll();
                var conversation = conversations.FirstOrDefault(c => c.MRChat != null);
                var merge = conversation.ListOfMerge.FirstOrDefault(m =>
                    Regex.Match(m.MrUrl, TICKET_NUMBER_PATTERN).Value.Equals(ticketNumber.ToString()));

                var canMrChanged = EditCommand.CanMrBeChanged(merge);

                if (canMrChanged)
                {
                    UpdateCommand(GetUserId(update), CommandId, string.Empty);
                    requestMessage = $"Please input new description for MR {ticketNumber}";
                    responseMessage.ReplyMarkup = new ReplyKeyboardHide() {IsHideKeyboard = true};
                }
                else
                {
                    requestMessage = "Please select MR number from collection";
                }
            }
            else
            {
                requestMessage = "Please input MR number";
            }

            responseMessage.Text = requestMessage;

            _telegram.SendMessageAsync(responseMessage).ConfigureAwait(false);

            _logger.Info("EditMergeNumberActionSubCommand - End", GetUserId(update));
        }

        public override async Task WorkOnAnswerAsync(Update update)
        {
            _logger.Info("EditMergeNumberActionSubCommand - Start", GetUserId(update));

            var newDescription = update.Message.Text;

            var number = GetCommand(GetUserId(update)).FirstOrDefault(c => c.Command.Equals(EditCommand.COMMANDID));

            if (int.TryParse(number.Answer, out int mrNumber))
            {
                var conversations = _dbContext.Conversations.GetAll();
                var conversation = conversations.FirstOrDefault(c => c.MRChat != null);

                var mr = conversation.ListOfMerge.FirstOrDefault(c =>
                    Regex.Match(c.MrUrl, TICKET_NUMBER_PATTERN).Value.Equals(number.Answer));

                var users = _dbContext.Users.GetAll();

                var returnedMessage = new EditMessageTextRequest()
                {
                    ChatId = conversation.MRChat.Id.ToString(),
                    FormattingMessageType = FormattingMessageType.Default,
                };

                if (!mr.VersionedSetting.Any())
                {
                    mr.Description = newDescription;
                    mr.IsHadAlreadyChange = true;

                    returnedMessage.AddButtonForRequest(
                        mr.MrUrl,
                        mr.TicketsUrl.Split(';').ToList(),
                        mr.Reactions.Count(c => c.ReactionType == ReactionType.Like),
                        mr.Reactions.Count(c => c.ReactionType == ReactionType.DisLike));

                    returnedMessage.ChatId = conversation.MRChat.Id;
                    returnedMessage.Text = $"{mr.Description} \nby {users.FirstOrDefault(c => c.UserId.Equals(mr.OwnerId)).Name}";
                    returnedMessage.EditMessageId = mr.TelegramMessageId;
                }
                else
                {
                    var versionedMr = mr.VersionedSetting.FirstOrDefault(v =>
                        v.PublishDate == mr.VersionedSetting.Max(s => s.PublishDate));

                    versionedMr.Description = newDescription;

                    returnedMessage.AddButtonForRequest(
                        mr.MrUrl,
                        mr.TicketsUrl.Split(';').ToList(),
                        versionedMr.Reactions.Count(c => c.ReactionType == ReactionType.Like),
                        versionedMr.Reactions.Count(c => c.ReactionType == ReactionType.DisLike));

                    returnedMessage.ChatId = conversation.MRChat.Id;
                    returnedMessage.Text = $"{versionedMr.Description} \nby {users.FirstOrDefault(c => c.UserId.Equals(mr.OwnerId)).Name}";
                    returnedMessage.EditMessageId = versionedMr.Id;
                }

                await _telegram.EditMessageAsync(returnedMessage).ConfigureAwait(false);
                await _telegram.SendMessageAsync(new SendMessageRequest()
                {
                    ChatId = update.Message.Chat.Id.ToString(),
                    Text = $"Done 😊",
                    FormattingMessageType = FormattingMessageType.Markdown,
                    ReplyMarkup = new ReplyKeyboardHide() { IsHideKeyboard = true },
                }).ConfigureAwait(false);

                _dbContext.Conversations.Update(conversation);
                ClearCommands(GetUserId(update));
            }

            _logger.Info("EditMergeNumberActionSubCommand - End", GetUserId(update));
        }
    }
}
