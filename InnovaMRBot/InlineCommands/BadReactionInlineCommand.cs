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
using TelegramBotApi.Telegram;
using TelegramBotApi.Telegram.Request;

namespace InnovaMRBot.InlineCommands
{
    public class BadReactionInlineCommand : BaseInlineCommand
    {
        public BadReactionInlineCommand(Telegram telegramService, UnitOfWork dbContext, Action<Guid, DateTime, ActionType> addAction, Logger logger) 
            : base(telegramService, dbContext, addAction, logger)
        {
        }

        public override bool IsThisInlineCommand(string data)
        {
            return data.Equals(Glossary.InlineAction.BAD_REACTION) ||
                   data.StartsWith(Glossary.InlineAction.BAD_REACTION_MR);
        }

        public override async Task WorkerAsync(Update update, string messageId)
        {
            _logger.Info("BadReactionInlineCommand - Start", update.CallbackQuery.Sender.Id.ToString());

            var conversationId = update.CallbackQuery.Message.Chat.Id.ToString();

            var returnedMessage = new EditMessageTextRequest()
            {
                ChatId = conversationId,
                FormattingMessageType = FormattingMessageType.Default,
            };

            if (string.IsNullOrEmpty(messageId))
            {
                messageId = update.CallbackQuery.Data.StartsWith(Glossary.InlineAction.BAD_REACTION_MR) ? update.CallbackQuery.Data.Replace(Glossary.InlineAction.BAD_REACTION_MR, string.Empty) : update.CallbackQuery.Message.Id.ToString();
            }

            var conversations = _dbContext.Conversations.GetAll();

            var conversation = conversations.FirstOrDefault(c => c.MRChat != null);

            if (conversation == null) return;

            var needMr = conversation.ListOfMerge.FirstOrDefault(m => m.TelegramMessageId.Equals(messageId));

            var userId = update.CallbackQuery.Sender.Id.ToString();
            var users = _dbContext.Users.GetAll();
            var needUser = SaveIfNeedUser(update.CallbackQuery.Sender);

            //WatchBlock
            {
                if (!string.IsNullOrEmpty(needUser.ChatId))
                {
                    var actions = _dbContext.Actions.GetAll();
                    var action = actions.FirstOrDefault(a =>
                        a.MessageId == update.CallbackQuery.Message.Id.ToString() && a.ActionFor == needUser.ChatId && a.ActionMethod.Equals(Glossary.ActionType.WATCH_NOTIFICATION));

                    if (action != null)
                    {
                        action.IsActive = false;

                        _dbContext.Actions.Update(action);

                        _scheduleAction.Invoke(action.Id, DateTime.MinValue, ActionType.Remove);
                    }
                }
            }

            if (needMr != null)
            {
                if (needMr.Reactions.Any(r => r.ReactionType == ReactionType.DisLike && r.UserId.Equals(userId)))
                {
                    needMr.Reactions.RemoveAll(r => r.ReactionType == ReactionType.DisLike && r.UserId.Equals(userId));

                    _telegramService.SendCallbackAnswerAsync(new AnswerCallbackQueryRequest()
                    {
                        IsNeedShowAlert = true,
                        Text = "Your reaction has been cancelled",
                        CallbackId = update.CallbackQuery.Id,
                    }).ConfigureAwait(false);

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

                    _dbContext.Conversations.Update(conversation);
                    _dbContext.Save();
                    return;
                }

                if (needMr.OwnerId.Equals(needUser.UserId))
                {
                    _telegramService.SendCallbackAnswerAsync(new AnswerCallbackQueryRequest()
                    {
                        IsNeedShowAlert = true,
                        Text = "You couldn't mark your MR 😊",
                        CallbackId = update.CallbackQuery.Id,
                    }).ConfigureAwait(false);

                    return;
                }

                if (needMr.Reactions.Any(r => r.ReactionType == ReactionType.Watch && r.UserId.Equals(userId)))
                {
                    needMr.Reactions.RemoveAll(r => r.ReactionType == ReactionType.Watch && r.UserId.Equals(userId));
                    //TODO: cancel watch
                }

                var reaction = new MessageReaction()
                {
                    ReactionTime = DateTimeOffset.UtcNow,
                    UserId = needUser.UserId,
                    ReactionType = ReactionType.DisLike,
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

                if (!string.IsNullOrEmpty(needUser.ChatId) && needUser.ChatId.Equals(update.CallbackQuery.Message.Chat.Id.ToString()))
                {
                    _telegramService.RemoveMessageAsync(new RemoveMessageRequest()
                    {
                        ChatId = update.CallbackQuery.Message.Chat.Id.ToString(),
                        MessageId = update.CallbackQuery.Message.Id.ToString(),
                    }).ConfigureAwait(false);
                }

                _telegramService.EditMessageAsync(returnedMessage).ConfigureAwait(false);

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
                            $"User *{needUser.Name}* commented your MR *{new Regex(MR_REMOVE_PATTERN).Replace(needMr.MrUrl, string.Empty)}* or will send you a message 😊",
                        FormattingMessageType = FormattingMessageType.Markdown,
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

                    if (versionOffMr.Reactions.Any(r => r.ReactionType == ReactionType.DisLike && r.UserId.Equals(userId)))
                    {
                        versionOffMr.Reactions.RemoveAll(r => r.ReactionType == ReactionType.DisLike && r.UserId.Equals(userId));

                        _telegramService.SendCallbackAnswerAsync(new AnswerCallbackQueryRequest()
                        {
                            IsNeedShowAlert = true,
                            Text = "Your reaction has been cancelled",
                            CallbackId = update.CallbackQuery.Id,
                        }).ConfigureAwait(false);

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

                        _dbContext.Conversations.Update(conversation);
                        _dbContext.Save();
                        return;
                    }

                    if (versionedMr.OwnerId.Equals(needUser.UserId))
                    {
                        await _telegramService.SendCallbackAnswerAsync(new AnswerCallbackQueryRequest()
                        {
                            IsNeedShowAlert = true,
                            Text = "You couldn't mark your MR 😊",
                            CallbackId = update.CallbackQuery.Id,
                        });

                        return;
                    }

                    if (versionOffMr.Reactions.Any(r => r.ReactionType == ReactionType.Watch && r.UserId.Equals(userId)))
                    {
                        versionOffMr.Reactions.RemoveAll(r => r.ReactionType == ReactionType.Watch && r.UserId.Equals(userId));
                        //TODO: cancel watch
                    }

                    var reaction = new MessageReaction()
                    {
                        ReactionTime = DateTimeOffset.UtcNow,
                        UserId = needUser.UserId,
                        ReactionType = ReactionType.DisLike,
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

                    if (!string.IsNullOrEmpty(needUser.ChatId) && needUser.ChatId.Equals(update.CallbackQuery.Message.Chat.Id.ToString()))
                    {
                        _telegramService.RemoveMessageAsync(new RemoveMessageRequest()
                        {
                            ChatId = update.CallbackQuery.Message.Chat.Id.ToString(),
                            MessageId = update.CallbackQuery.Message.Id.ToString(),
                        }).ConfigureAwait(false);
                    }

                    _telegramService.EditMessageAsync(returnedMessage).ConfigureAwait(false);

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
                                $"User *{needUser.Name}* commented your MR *{new Regex(MR_REMOVE_PATTERN).Replace(versionedMr.MrUrl, string.Empty)}* or will send you a message 😊",
                            FormattingMessageType = FormattingMessageType.Markdown,
                        };

                        _telegramService.SendMessageAsync(requestMessage).ConfigureAwait(false);
                    }
                }
            }

            _dbContext.Conversations.Update(conversation);
            _dbContext.Save();
            _logger.Info("BadReactionInlineCommand - End", update.CallbackQuery.Sender.Id.ToString());
        }
    }
}
