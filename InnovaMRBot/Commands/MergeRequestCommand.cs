using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using InnovaMRBot.Helpers;
using InnovaMRBot.Models;
using InnovaMRBot.Models.Enum;
using InnovaMRBot.Repository;
using InnovaMRBot.Services;
using TelegramBotApi.Extension;
using TelegramBotApi.Models;
using TelegramBotApi.Models.Keyboard;
using TelegramBotApi.Telegram;
using TelegramBotApi.Telegram.Request;

namespace InnovaMRBot.Commands
{
    public class MergeRequestCommand : BaseCommand
    {
        private const string MR_PATTERN = @"https?:\/\/gitlab.fortia.fr\/Fortia\/Innova\/merge_requests\/[0-9]+";

        private const string MR_WITH_DIFF_PATTERN = @"https?:\/\/gitlab.fortia.fr\/Fortia\/Innova\/merge_requests\/[0-9]+\/diffs";

        private const string MR_WITH_SLASH_PATTERN = @"https?:\/\/gitlab.fortia.fr\/Fortia\/Innova\/merge_requests\/[0-9]+\/";

        private const string MR_WITH_COMMITS_PATTERN = @"https?:\/\/gitlab.fortia.fr\/Fortia\/Innova\/merge_requests\/[0-9]+\/commits";

        private static readonly char[] trimmidChars = new char[] { '\r', '\n', ' ' };

        private const int REVIEW_MR_DELAY_MINUTES = 60;

        private ChatStateService _chatStateService;

        private readonly List<string> _changesNotation = new List<string>()
        {
            "UPDATED",
            "ИЗМЕНЕНО"
        };

        public MergeRequestCommand(Telegram telegram, UnitOfWork dbContext) : base(telegram, dbContext)
        {
            CommandId = "mergerequestmaincommand";
        }

        protected override EqualType GetType()
        {
            return EqualType.Pattern;
        }

        protected override string GetCommandString()
        {
            return string.Empty;
        }

        protected override string GetPattern()
        {
            return MR_PATTERN;
        }

        public void SetChatStateService(ChatStateService service)
        {
            _chatStateService = service;
        }

        public override async Task WorkerAsync(Update update)
        {
            var convesationId = update.Message.Chat.Id.ToString();

            var responseMessage = new SendMessageRequest
            {
                ChatId = convesationId,
            };

            var responseMessageForUser = new SendMessageRequest
            {
                ChatId = convesationId,
            };

            var messageText = update.Message.Text;

            var mrUrl = new Regex(MR_PATTERN).Match(messageText).Value;

            if (string.IsNullOrEmpty(mrUrl))
            {
                SetupError(MergeErrorType.MRLink, responseMessageForUser);
                return;
            }

            if (!new Regex(TICKET_PATTERN).IsMatch(messageText))
            {
                SetupError(MergeErrorType.TicketLink, responseMessageForUser);
                return;
            }

            messageText = OptimizeText(messageText);

            var conversations = _dbContext.Conversations.GetAll();

            var conversation = conversations.FirstOrDefault(c => c.MRChat != null);

            if (conversation == null) return;

            var needUser = SaveIfNeedUser(update.Message.Sender);
            UpdateUserChatIdNeed(needUser, convesationId);

            if (IsMrContaince(mrUrl))
            {
                // for updatedTicket
                var needMr = conversation.ListOfMerge.FirstOrDefault(m => m.MrUrl.Equals(mrUrl));

                // ReSharper disable once PossibleNullReferenceException
                needMr.CountOfChange++;

                var description = new Regex(MR_PATTERN).Replace(new Regex(TICKET_PATTERN).Replace(messageText, string.Empty), string.Empty).Trim(trimmidChars);

                description = RemoveUpdateHeader(description);

                if (string.IsNullOrEmpty(description))
                {
                    SetupError(MergeErrorType.Description, responseMessageForUser);
                    return;
                }

                var versionedTicket = new VersionedMergeRequest
                {
                    OwnerMergeId = needMr.TelegramMessageId,
                    PublishDate = DateTimeOffset.UtcNow,
                    AllDescription = messageText,
                    Description = description,
                };

                responseMessage.ChatId = conversation.MRChat.Id;
                responseMessage.Text = $"{description.Trim(trimmidChars)} \nby {needUser.Name}";
                responseMessage.AddButtonForRequest(mrUrl, needMr.TicketsUrl.Split(';').ToList());

                var resMessage = await _telegram.SendMessageAsync(responseMessage);

                // Add action
                var action = new Models.Action()
                {
                    Name = "Review",
                    Id = Guid.NewGuid(),
                    MessageId = resMessage.Id.ToString(),
                    IsActive = true,
                    ExecDate = DateTime.UtcNow.AddMinutes(REVIEW_MR_DELAY_MINUTES),
                    ActionMethod = Glossary.ActionType.REVIEW_NOTIFICATION,
                };

                _dbContext.Actions.Create(action);
                _dbContext.Save();

                _chatStateService.SchedulerAction(action.Id, ActionType.Add);
                
                versionedTicket.Id = resMessage.Id.ToString();
                responseMessageForUser.Text = "Well done! I'll send it 😊" +
                                              $"Your mr number {Regex.Match(mrUrl, TICKET_NUMBER_PATTERN)}";

                responseMessageForUser.ReplyMarkup = new InlineKeyboardMarkup()
                {
                    InlineKeyboardButtons = new List<List<InlineKeyboardButton>>()
                    {
                        new List<InlineKeyboardButton>()
                        {
                            new InlineKeyboardButton()
                            {
                                Text = "Edit",
                                CallbackData = $"/edit {Regex.Match(mrUrl, TICKET_NUMBER_PATTERN)}",
                            },
                            // TODO: add watch button
                        },
                    },
                };

                _telegram.SendMessageAsync(responseMessageForUser).ConfigureAwait(false);

                if (needUser.CanRemoveOldMr)
                {
                    if (needMr.VersionedSetting.Any())
                    {
                        var lastVersion = needMr.VersionedSetting.FirstOrDefault(v =>
                            v.PublishDate == needMr.VersionedSetting.Max(s => s.PublishDate));

                        _telegram.RemoveMessageAsync(new RemoveMessageRequest()
                        {
                            ChatId = conversation.MRChat.Id,
                            MessageId = lastVersion.Id,
                        }).ConfigureAwait(false);
                    }
                    else
                    {
                        _telegram.RemoveMessageAsync(new RemoveMessageRequest()
                        {
                            ChatId = conversation.MRChat.Id,
                            MessageId = needMr.TelegramMessageId,
                        }).ConfigureAwait(false);
                    }
                }

                needMr.VersionedSetting.Add(versionedTicket);

                _dbContext.Conversations.Update(conversation);
            }
            else
            {
                var mrMessage = new MergeSetting { MrUrl = mrUrl, AllText = messageText };

                var ticketMatches = new Regex(TICKET_PATTERN).Matches(messageText);

                var description = messageText.Replace(mrUrl, string.Empty);

                foreach (Match ticketMatch in ticketMatches)
                {
                    mrMessage.TicketsUrl += ticketMatch.Value + ";";
                    description = description.Replace(ticketMatch.Value, string.Empty);
                }

                mrMessage.PublishDate = DateTimeOffset.UtcNow;

                description = RemoveUpdateHeader(description);

                if (string.IsNullOrEmpty(description))
                {
                    SetupError(MergeErrorType.Description, responseMessageForUser);
                    return;
                }

                mrMessage.Description = description.Trim(trimmidChars);
                mrMessage.OwnerId = needUser.UserId;

                responseMessageForUser.Text = "Well done! I'll send it 😊" +
                                              $"Your mr number {Regex.Match(mrUrl, TICKET_NUMBER_PATTERN)}";

                responseMessageForUser.ReplyMarkup = new InlineKeyboardMarkup()
                {
                    InlineKeyboardButtons = new List<List<InlineKeyboardButton>>()
                    {
                        new List<InlineKeyboardButton>()
                        {
                            new InlineKeyboardButton()
                            {
                                Text = "Edit",
                                CallbackData = $"/edit {Regex.Match(mrUrl, TICKET_NUMBER_PATTERN)}",
                            },
                            // TODO: add watch button
                        },
                    },
                };

                _telegram.SendMessageAsync(responseMessageForUser).ConfigureAwait(false);

                responseMessage.AddButtonForRequest(mrUrl, mrMessage.TicketsUrl.Split(';').ToList());

                responseMessage.ChatId = conversation.MRChat.Id;
                responseMessage.Text = $"{description.Trim(trimmidChars)} \nby {needUser.Name}";

                var resMessage = await _telegram.SendMessageAsync(responseMessage);

                // Add action
                var action = new Models.Action()
                {
                    Name = "Review",
                    Id = Guid.NewGuid(),
                    MessageId = resMessage.Id.ToString(),
                    IsActive = true,
                    ExecDate = DateTime.UtcNow.AddMinutes(REVIEW_MR_DELAY_MINUTES),
                    ActionMethod = Glossary.ActionType.REVIEW_NOTIFICATION,
                };

                _dbContext.Actions.Create(action);
                _dbContext.Save();

                _chatStateService.SchedulerAction(action.Id, ActionType.Add);

                mrMessage.TelegramMessageId = resMessage.Id.ToString();

                conversation.ListOfMerge.Add(mrMessage);

                _dbContext.Conversations.Update(conversation);
            }

            _dbContext.Save();
        }

        private void SetupError(MergeErrorType type, SendMessageRequest responseMessageForUser)
        {
            switch (type)
            {
                case MergeErrorType.MRLink:
                    responseMessageForUser.Text = "Please add MR link to message, thanks 😊";
                    break;
                case MergeErrorType.TicketLink:
                    responseMessageForUser.Text = "Please add Ticket link to message, thanks 😊";
                    break;
                case MergeErrorType.Description:
                    responseMessageForUser.Text = "Please add description for your MR to message, thanks 😊";
                    break;
            }

            _telegram.SendMessageAsync(responseMessageForUser).ConfigureAwait(false);
        }

        private string RemoveUpdateHeader(string description)
        {
            var lineOfMessage = description.Split('\r').ToList();
            var firstLine = lineOfMessage.FirstOrDefault();

            if (_changesNotation.Any(c => c.Equals(firstLine, StringComparison.InvariantCultureIgnoreCase)))
            {
                lineOfMessage.RemoveAt(0);
                description = string.Join('\r', lineOfMessage);
            }

            return description;
        }

        private string OptimizeText(string messageText)
        {
            if (new Regex(MR_WITH_DIFF_PATTERN).IsMatch(messageText))
            {
                messageText = new Regex(MR_WITH_DIFF_PATTERN).Replace(messageText, string.Empty).Trim(trimmidChars);
            }
            else if (new Regex(MR_WITH_COMMITS_PATTERN).IsMatch(messageText))
            {
                messageText = new Regex(MR_WITH_COMMITS_PATTERN).Replace(messageText, string.Empty).Trim(trimmidChars);
            }
            else if (new Regex(MR_WITH_SLASH_PATTERN).IsMatch(messageText))
            {
                messageText = new Regex(MR_WITH_SLASH_PATTERN).Replace(messageText, string.Empty).Trim(trimmidChars);
            }

            return messageText;
        }

        private bool IsMrContaince(string mrUrl)
        {
            if (string.IsNullOrEmpty(mrUrl)) return false;

            var conversations = _dbContext.Conversations.GetAll();

            if (conversations == null || !conversations.Any()) return false;

            var mrChat = conversations.FirstOrDefault(c => c.MRChat != null);

            if (mrChat?.ListOfMerge == null || !mrChat.ListOfMerge.Any()) return false;

            return mrChat.ListOfMerge.Any(c => c.MrUrl.Equals(mrUrl, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
