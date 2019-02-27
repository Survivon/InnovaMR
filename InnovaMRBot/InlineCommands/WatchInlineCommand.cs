using InnovaMRBot.Helpers;
using InnovaMRBot.Models;
using InnovaMRBot.Models.Enum;
using InnovaMRBot.Repository;
using InnovaMRBot.Services;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TelegramBotApi.Extension;
using TelegramBotApi.Models;
using TelegramBotApi.Models.Enum;
using TelegramBotApi.Models.Keyboard;
using TelegramBotApi.Telegram;
using TelegramBotApi.Telegram.Request;

namespace InnovaMRBot.InlineCommands
{
    public class WatchInlineCommand : BaseInlineCommand
    {
        private const int WATCH_TIMEOUT_MINUTE = 30;

        public WatchInlineCommand(Telegram telegramService, UnitOfWork dbContext, Action<Guid, DateTime, ActionType> addAction, Logger logger)
            : base(telegramService, dbContext, addAction, logger)
        {
        }

        public override bool IsThisInlineCommand(string data)
        {
            return data.Equals(Glossary.InlineAction.START_WATCH);
        }

        public override async Task WorkerAsync(Update update, string messageId)
        {
            _logger.Info("WatchInlineCommand - Start", update.CallbackQuery.Sender.Id.ToString());

            var conversationId = update.CallbackQuery.Message.Chat.Id.ToString();

            var returnedMessage = new EditMessageTextRequest()
            {
                ChatId = conversationId,
                FormattingMessageType = FormattingMessageType.Default,
            };

            if (string.IsNullOrEmpty(messageId))
            {
                messageId = update.CallbackQuery.Message.Id.ToString();
            }

            var conversations = _dbContext.Conversations.GetAll();

            var conversation = conversations.FirstOrDefault(c => c.MRChat != null);

            if (conversation == null) return;

            var needMr = conversation.ListOfMerge.FirstOrDefault(m => m.TelegramMessageId.Equals(messageId));

            var userId = update.CallbackQuery.Sender.Id.ToString();
            var users = _dbContext.Users.GetAll();
            var needUser = SaveIfNeedUser(update.CallbackQuery.Sender);

            if (needMr != null)
            {
                if (needMr.Reactions.Any(r => r.ReactionType == ReactionType.Watch && r.UserId.Equals(userId)))
                {
                    _telegramService.SendCallbackAnswerAsync(new AnswerCallbackQueryRequest()
                    {
                        IsNeedShowAlert = true,
                        Text = "Your couldn't unmarked this MR, please set reaction 👍 or 🚫",
                        CallbackId = update.CallbackQuery.Id,
                    }).ConfigureAwait(false);

                    return;
                }

                if (needMr.OwnerId.Equals(needUser.UserId))
                {
                    new MergeStatisticInlineCommand(_telegramService, _dbContext, _scheduleAction, _logger)
                        .WorkerAsync(update, messageId).ConfigureAwait(false);
                    return;
                }

                var reaction = new MessageReaction()
                {
                    ReactionTime = DateTimeOffset.UtcNow,
                    UserId = needUser.UserId,
                    ReactionType = ReactionType.Watch,
                };

                reaction.SetReactionInMinutes(needMr.PublishDate.Value);

                needMr.Reactions.Add(reaction);

                returnedMessage.AddButtonForRequest(
                    needMr.MrUrl,
                    needMr.TicketsUrl.Split(';').ToList(),
                    needMr.Reactions.Count(c => c.ReactionType == ReactionType.Like),
                    needMr.Reactions.Count(c => c.ReactionType == ReactionType.DisLike),
                    needMr.Reactions.Count(c => c.ReactionType == ReactionType.Watch));

                returnedMessage.ChatId = conversation.MRChat.Id;
                returnedMessage.Text = $"{needMr.Description} \nby {users.FirstOrDefault(c => c.UserId.Equals(needMr.OwnerId)).Name}";
                returnedMessage.EditMessageId = messageId;

                _telegramService.EditMessageAsync(returnedMessage).ConfigureAwait(false);
                _telegramService.SendCallbackAnswerAsync(new AnswerCallbackQueryRequest()
                {
                    CallbackId = update.CallbackQuery.Id,
                }).ConfigureAwait(false);

                if (needMr.Owner == null)
                {
                    needMr.Owner = users.FirstOrDefault(u => u.UserId.Equals(needMr.OwnerId));
                }

                var chatWithMrsUser = needMr.Owner.ChatId;
                if (!string.IsNullOrEmpty(chatWithMrsUser))
                {
                    var requestMessage = new SendMessageRequest()
                    {
                        ChatId = chatWithMrsUser,
                        Text =
                            $"Your MR {new Regex(MR_REMOVE_PATTERN).Replace(needMr.MrUrl, string.Empty)} now watch {users.FirstOrDefault(u => u.UserId.Equals(update.CallbackQuery.Sender.Id.ToString())).Name} 😊",
                        FormattingMessageType = FormattingMessageType.Markdown,
                        ReplyMarkup = new ReplyKeyboardHide() { IsHideKeyboard = true },
                    };

                    _telegramService.SendMessageAsync(requestMessage).ConfigureAwait(false);
                }
            }
            else
            {
                var versionOffMr = conversation.ListOfMerge.SelectMany(m => m.VersionedSetting)
                    .FirstOrDefault(v => v.Id.Equals(messageId));
                if (versionOffMr != null)
                {
                    var versionedMr =
                        conversation.ListOfMerge.FirstOrDefault(
                            m => m.VersionedSetting.Any(v => v.Id.Equals(messageId)));

                    if (versionOffMr.Reactions.Any(r => r.ReactionType == ReactionType.Watch && r.UserId.Equals(userId)))
                    {
                        _telegramService.SendCallbackAnswerAsync(new AnswerCallbackQueryRequest()
                        {
                            IsNeedShowAlert = true,
                            Text = "Your couldn't unmarked this MR, please set reaction 👍 or 🚫",
                            CallbackId = update.CallbackQuery.Id,
                        }).ConfigureAwait(false);

                        return;
                    }

                    if (versionedMr.OwnerId.Equals(needUser.UserId))
                    {
                        new MergeStatisticInlineCommand(_telegramService, _dbContext, _scheduleAction, _logger)
                            .WorkerAsync(update, messageId).ConfigureAwait(false);
                        return;
                    }

                    var reaction = new MessageReaction()
                    {
                        ReactionTime = DateTimeOffset.UtcNow,
                        UserId = needUser.UserId,
                        ReactionType = ReactionType.Watch,
                    };

                    reaction.SetReactionInMinutes(versionOffMr.PublishDate.Value);

                    versionOffMr.Reactions.Add(reaction);

                    returnedMessage.AddButtonForRequest(
                        versionedMr.MrUrl,
                        versionedMr.TicketsUrl.Split(';').ToList(),
                        versionOffMr.Reactions.Count(c => c.ReactionType == ReactionType.Like),
                        versionOffMr.Reactions.Count(c => c.ReactionType == ReactionType.DisLike),
                        versionOffMr.Reactions.Count(c => c.ReactionType == ReactionType.Watch));

                    returnedMessage.ChatId = conversation.MRChat.Id;
                    returnedMessage.Text = $"{versionOffMr.Description} \nby {users.FirstOrDefault(c => c.UserId.Equals(versionedMr.OwnerId)).Name}";
                    returnedMessage.EditMessageId = messageId;

                    _telegramService.EditMessageAsync(returnedMessage).ConfigureAwait(false);
                    _telegramService.SendCallbackAnswerAsync(new AnswerCallbackQueryRequest()
                    {
                        CallbackId = update.CallbackQuery.Id,
                    }).ConfigureAwait(false);

                    if (versionedMr.Owner == null)
                    {
                        versionedMr.Owner = users.FirstOrDefault(u => u.UserId.Equals(versionedMr.OwnerId));
                    }

                    var chatWithMrsUser = versionedMr.Owner.ChatId;
                    if (!string.IsNullOrEmpty(chatWithMrsUser))
                    {
                        var requestMessage = new SendMessageRequest()
                        {
                            ChatId = chatWithMrsUser,
                            Text =
                                $"Your MR {new Regex(MR_REMOVE_PATTERN).Replace(versionedMr.MrUrl, string.Empty)} now watch {users.FirstOrDefault(u => u.UserId.Equals(update.CallbackQuery.Sender.Id.ToString())).Name} 😊",
                            FormattingMessageType = FormattingMessageType.Markdown,
                            ReplyMarkup = new ReplyKeyboardHide() { IsHideKeyboard = true },
                        };

                        _telegramService.SendMessageAsync(requestMessage).ConfigureAwait(false);
                    }
                }
            }

            // Add action
            var action = new Models.Action()
            {
                Name = "Watch",
                Id = Guid.NewGuid(),
                MessageId = update.CallbackQuery.Message.Id.ToString(),
                ActionFor = needUser.ChatId,
                IsActive = true,
                ExecDate = DateTime.UtcNow.AddMinutes(WATCH_TIMEOUT_MINUTE),
                ActionMethod = Glossary.ActionType.WATCH_NOTIFICATION,
            };

            _dbContext.Actions.Create(action);
            _dbContext.Conversations.Update(conversation);
            _dbContext.Save();

            _scheduleAction.Invoke(action.Id, action.ExecDate, ActionType.Add);
            _logger.Info("WatchInlineCommand - End", update.CallbackQuery.Sender.Id.ToString());
        }
    }
}
