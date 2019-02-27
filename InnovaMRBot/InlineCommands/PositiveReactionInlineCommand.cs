using InnovaMRBot.Helpers;
using InnovaMRBot.Models;
using InnovaMRBot.Models.Enum;
using InnovaMRBot.Repository;
using InnovaMRBot.Services;
using System;
using System.Collections.Generic;
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
    public class PositiveReactionInlineCommand : BaseInlineCommand
    {
        public PositiveReactionInlineCommand(Telegram telegramService, UnitOfWork dbContext, Action<Guid, DateTime, ActionType> addAction, Logger logger) : base(telegramService, dbContext, addAction, logger)
        {
        }

        public override bool IsThisInlineCommand(string data)
        {
            return data.Equals(Glossary.InlineAction.SUCCESS_REACTION) ||
                   data.StartsWith(Glossary.InlineAction.SUCCESS_REACTION_MR);
        }

        public override async Task WorkerAsync(Update update, string messageId)
        {
            _logger.Info("PositiveReactionInlineCommand - Start", update.CallbackQuery.Sender.Id.ToString());

            var conversationId = update.CallbackQuery.Message.Chat.Id.ToString();

            var returnedMessage = new EditMessageTextRequest()
            {
                ChatId = conversationId,
                FormattingMessageType = FormattingMessageType.Default,
            };

            if (string.IsNullOrEmpty(messageId))
            {
                messageId = update.CallbackQuery.Data.StartsWith(Glossary.InlineAction.SUCCESS_REACTION_MR) ? update.CallbackQuery.Data.Replace(Glossary.InlineAction.SUCCESS_REACTION_MR, string.Empty) : update.CallbackQuery.Message.Id.ToString();
            }

            var conversations = _dbContext.Conversations.GetAll();

            var conversation = conversations.FirstOrDefault(c => c.MRChat != null);

            if (conversation == null) return;

            var needMr = conversation.ListOfMerge.FirstOrDefault(m => m.TelegramMessageId.Equals(messageId));

            var userId = update.CallbackQuery.Sender.Id.ToString();
            var users = _dbContext.Users.GetAll();
            var actions = _dbContext.Actions.GetAll();
            var needUser = SaveIfNeedUser(update.CallbackQuery.Sender);

            //WatchBlock
            {
                if (!string.IsNullOrEmpty(needUser.ChatId))
                {
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
                if (needMr.Reactions.Any(r => r.ReactionType == ReactionType.Like && r.UserId.Equals(userId)))
                {
                    needMr.Reactions.RemoveAll(r => r.ReactionType == ReactionType.Like && r.UserId.Equals(userId));

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
                    new MergeStatisticInlineCommand(_telegramService, _dbContext, _scheduleAction, _logger)
                        .WorkerAsync(update, messageId).ConfigureAwait(false);
                    return;
                }

                if (needMr.Reactions.Any(r => r.ReactionType == ReactionType.Watch && r.UserId.Equals(userId)))
                {
                    needMr.Reactions.RemoveAll(r => r.ReactionType == ReactionType.Watch && r.UserId.Equals(userId));
                }

                var reaction = new MessageReaction()
                {
                    ReactionTime = DateTimeOffset.UtcNow,
                    UserId = needUser.UserId,
                    ReactionType = ReactionType.Like,
                };

                reaction.SetReactionInMinutes(needMr.PublishDate.Value);

                needMr.Reactions.Add(reaction);

                needMr.Reactions.RemoveAll(r => r.ReactionType == ReactionType.DisLike && r.UserId.Equals(userId));

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

                await _telegramService.EditMessageAsync(returnedMessage);

                if (needMr.Reactions.Count(r => r.ReactionType == ReactionType.Like) == 2)
                {
                    //ReviewBlock
                    {
                        var action = actions.FirstOrDefault(a =>
                            a.MessageId == update.CallbackQuery.Message.Id.ToString() &&
                            string.IsNullOrEmpty(a.ActionFor));

                        if (action != null)
                        {
                            action.IsActive = false;
                            _dbContext.Actions.Update(action);

                            _scheduleAction.Invoke(action.Id, DateTime.MinValue, ActionType.Remove);
                        }
                    }

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
                                $"Yor MR {new Regex(MR_REMOVE_PATTERN).Replace(needMr.MrUrl, string.Empty)} is get 2 likes. Please, remove WIP status 😊",
                            FormattingMessageType = FormattingMessageType.Markdown,
                            ReplyMarkup = new InlineKeyboardMarkup()
                            {
                                InlineKeyboardButtons = new List<List<InlineKeyboardButton>>()
                                {
                                    new List<InlineKeyboardButton>()
                                    {
                                        new InlineKeyboardButton()
                                        {
                                            Text = "MR link",
                                            Url = needMr.MrUrl,
                                        },
                                    },
                                },
                            },
                        };

                        _telegramService.SendMessageAsync(requestMessage).ConfigureAwait(false);
                    }
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

                    if (versionOffMr.Reactions.Any(r => r.ReactionType == ReactionType.Like && r.UserId.Equals(userId)))
                    {
                        versionOffMr.Reactions.RemoveAll(r => r.ReactionType == ReactionType.Like && r.UserId.Equals(userId));

                        _telegramService.SendCallbackAnswerAsync(new AnswerCallbackQueryRequest()
                        {
                            IsNeedShowAlert = true,
                            Text = "Your reaction has been cancelled",
                            CallbackId = update.CallbackQuery.Id,
                        }).ConfigureAwait(false);

                        versionOffMr.Reactions.RemoveAll(r => r.ReactionType == ReactionType.DisLike && r.UserId == needUser.UserId);

                        returnedMessage.AddButtonForRequest(
                            versionedMr.MrUrl,
                            versionedMr.TicketsUrl.Split(';').ToList(),
                            versionOffMr.Reactions.Count(c => c.ReactionType == ReactionType.Like),
                            versionOffMr.Reactions.Count(c => c.ReactionType == ReactionType.DisLike),
                            versionedMr.Reactions.Count(c => c.ReactionType == ReactionType.Watch));

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
                        new MergeStatisticInlineCommand(_telegramService, _dbContext, _scheduleAction, _logger)
                            .WorkerAsync(update, messageId).ConfigureAwait(false);
                        return;
                    }

                    if (versionOffMr.Reactions.Any(r => r.ReactionType == ReactionType.Watch && r.UserId.Equals(userId)))
                    {
                        versionOffMr.Reactions.RemoveAll(r => r.ReactionType == ReactionType.Watch && r.UserId.Equals(userId));
                    }

                    var reaction = new MessageReaction()
                    {
                        ReactionTime = DateTimeOffset.UtcNow,
                        UserId = needUser.UserId,
                        ReactionType = ReactionType.Like,
                    };

                    reaction.SetReactionInMinutes(versionOffMr.PublishDate.Value);

                    versionOffMr.Reactions.Add(reaction);

                    returnedMessage.AddButtonForRequest(
                        versionedMr.MrUrl,
                        versionedMr.TicketsUrl.Split(';').ToList(),
                        versionOffMr.Reactions.Count(c => c.ReactionType == ReactionType.Like),
                        versionOffMr.Reactions.Count(c => c.ReactionType == ReactionType.DisLike),
                        versionedMr.Reactions.Count(c => c.ReactionType == ReactionType.Watch));

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

                    await _telegramService.EditMessageAsync(returnedMessage);

                    if (versionOffMr.Reactions.Count(r => r.ReactionType == ReactionType.Like) == 2)
                    {
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
                                    $"Yor MR {new Regex(MR_REMOVE_PATTERN).Replace(versionedMr.MrUrl, string.Empty)} is get 2 likes. Please, remove WIP status 😊",
                                FormattingMessageType = FormattingMessageType.Markdown,
                                ReplyMarkup = new InlineKeyboardMarkup()
                                {
                                    InlineKeyboardButtons = new List<List<InlineKeyboardButton>>()
                                    {
                                        new List<InlineKeyboardButton>()
                                        {
                                            new InlineKeyboardButton()
                                            {
                                                Text = "MR link",
                                                Url = versionedMr.MrUrl,
                                            },
                                        },
                                    },
                                },
                            };

                            _telegramService.SendMessageAsync(requestMessage).ConfigureAwait(false);
                        }
                    }
                }
            }

            _dbContext.Conversations.Update(conversation);
            _dbContext.Save();

            _logger.Info("PositiveReactionInlineCommand - End", update.CallbackQuery.Sender.Id.ToString());
        }
    }
}
