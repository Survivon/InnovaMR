using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using InnovaMRBot.Helpers;
using InnovaMRBot.Models;
using InnovaMRBot.Models.Enum;
using InnovaMRBot.Repository;
using TelegramBotApi.Extension;
using TelegramBotApi.Models;
using TelegramBotApi.Models.Enum;
using TelegramBotApi.Models.Keyboard;
using TelegramBotApi.Telegram;
using TelegramBotApi.Telegram.Request;
using User = InnovaMRBot.Models.User;

namespace InnovaMRBot.InlineActions
{
    public static class InlineAction
    {
        private const string MR_REMOVE_PATTERN = @"https?:\/\/gitlab.fortia.fr\/Fortia\/Innova\/merge_requests\/";

        private const int WATCH_TIMEOUT_MINUTE = 30;

        public static readonly Dictionary<string, Func<Update, Telegram, UnitOfWork, Action<Guid, ActionType>, string, Task>> Actions = new Dictionary<string, Func<Update, Telegram, UnitOfWork, Action<Guid, ActionType>, string, Task>>()
        {
            { Glossary.InlineAction.GET_STAT, GetMergeStatAsync },
            { Glossary.InlineAction.BAD_REACTION, SetupBadReactionAsync },
            { Glossary.InlineAction.SUCCESS_REACTION, SetupMessageReactionAsync },
            { Glossary.InlineAction.START_WATCH, StartWatchMergeAsync },
        };

        private static async Task GetMergeStatAsync(Update update, Telegram telegramService, UnitOfWork dbContext, Action<Guid, ActionType> addAction, string messageId)
        {
            if (string.IsNullOrEmpty(messageId))
            {
                messageId = update.CallbackQuery.Message.Id.ToString();
            }

            var conversations = dbContext.Conversations.GetAll();
            var conversation = conversations.FirstOrDefault(c => c.MRChat != null);

            if (conversation == null) return;

            var users = dbContext.Users.GetAll().ToList();
            var currentUser = SaveIfNeedUser(update.CallbackQuery.Sender, dbContext);

            var needMr = conversation.ListOfMerge.FirstOrDefault(m => m.TelegramMessageId.Equals(messageId));
            if (needMr != null)
            {
                var textForShare = new StringBuilder();
                textForShare.AppendLine($"Reaction for MR {new Regex(MR_REMOVE_PATTERN).Replace(needMr.MrUrl, string.Empty)} by {users.FirstOrDefault(c => c.UserId.Equals(needMr.OwnerId)).Name}");
                textForShare.AppendLine();

                textForShare.AppendLine(GetMrReaction(needMr.Reactions, users, currentUser));

                telegramService.SendCallbackAnswerAsync(new AnswerCallbackQueryRequest()
                {
                    IsNeedShowAlert = true,
                    Text = textForShare.ToString(),
                    CallbackId = update.CallbackQuery.Id,
                }).ConfigureAwait(false);
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

                    var textForShare = new StringBuilder();
                    textForShare.AppendLine($"Reaction for MR {new Regex(MR_REMOVE_PATTERN).Replace(versionedMr.MrUrl, string.Empty)} by {users.FirstOrDefault(c => c.UserId.Equals(versionedMr.OwnerId)).Name}");
                    textForShare.AppendLine();

                    textForShare.AppendLine(GetMrReaction(versionedMr.Reactions, users, currentUser));

                    telegramService.SendCallbackAnswerAsync(new AnswerCallbackQueryRequest()
                    {
                        IsNeedShowAlert = true,
                        Text = textForShare.ToString(),
                        CallbackId = update.CallbackQuery.Id,
                    }).ConfigureAwait(false);
                }
            }
        }

        private static string GetMrReaction(List<MessageReaction> reactions, List<User> users, User currentUser)
        {
            var textForShare = new StringBuilder();

            if (reactions.Any())
            {
                textForShare.AppendLine("Members Like reaction:");
                foreach (var messageReaction in reactions.Where(r => r.ReactionType == ReactionType.Like))
                {
                    textForShare.AppendLine(
                        $"{users.FirstOrDefault(c => c.UserId.Equals(messageReaction.UserId)).Name} in {messageReaction.ReactionTime.GetUserTime(currentUser)}");
                }

                textForShare.AppendLine("Members Block reaction:");
                foreach (var messageReaction in reactions.Where(r => r.ReactionType == ReactionType.DisLike))
                {
                    textForShare.AppendLine(
                        $"{users.FirstOrDefault(c => c.UserId.Equals(messageReaction.UserId)).Name} in {messageReaction.ReactionTime.GetUserTime(currentUser)}");
                }

                textForShare.AppendLine("Members Watch:");
                foreach (var messageReaction in reactions.Where(r => r.ReactionType == ReactionType.Watch))
                {
                    textForShare.AppendLine(
                        $"{users.FirstOrDefault(c => c.UserId.Equals(messageReaction.UserId)).Name} in {messageReaction.ReactionTime.GetUserTime(currentUser)}");
                }
            }
            else
            {
                textForShare.AppendLine("No reactions to this MR 😔");
            }

            return textForShare.ToString();
        }

        private static async Task SetupBadReactionAsync(Update update, Telegram telegramService, UnitOfWork dbContext, Action<Guid, ActionType> addAction, string messageId)
        {
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

            var conversations = dbContext.Conversations.GetAll();

            var conversation = conversations.FirstOrDefault(c => c.MRChat != null);

            if (conversation == null) return;

            var needMr = conversation.ListOfMerge.FirstOrDefault(m => m.TelegramMessageId.Equals(messageId));

            var userId = update.CallbackQuery.Sender.Id.ToString();
            var users = dbContext.Users.GetAll();
            var needUser = SaveIfNeedUser(update.CallbackQuery.Sender, dbContext);

            //WatchBlock
            {
                var actions = dbContext.Actions.GetAll();

                var action = actions.FirstOrDefault(a =>
                    a.MessageId == update.CallbackQuery.Message.Id.ToString() && a.ActionFor == needUser.ChatId);

                if (action != null)
                {
                    action.IsActive = false;

                    dbContext.Actions.Update(action);

                    addAction.Invoke(action.Id, ActionType.Remove);
                }
            }

            if (needMr != null)
            {
                if (needMr.Reactions.Any(r => r.ReactionType == ReactionType.DisLike && r.UserId.Equals(userId)))
                {
                    needMr.Reactions.RemoveAll(r => r.ReactionType == ReactionType.DisLike && r.UserId.Equals(userId));

                    telegramService.SendCallbackAnswerAsync(new AnswerCallbackQueryRequest()
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

                    telegramService.EditMessageAsync(returnedMessage).ConfigureAwait(false);

                    dbContext.Conversations.Update(conversation);
                    dbContext.Save();
                    return;
                }

                if (needMr.OwnerId.Equals(needUser.UserId))
                {
                    await telegramService.SendCallbackAnswerAsync(new AnswerCallbackQueryRequest()
                    {
                        IsNeedShowAlert = true,
                        Text = "You couldn't mark your MR 😊",
                        CallbackId = update.CallbackQuery.Id,
                    });

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

                if (needUser.ChatId.Equals(update.CallbackQuery.Message.Chat.Id.ToString()))
                {
                    telegramService.RemoveMessageAsync(new RemoveMessageRequest()
                    {
                        ChatId = update.CallbackQuery.Message.Chat.Id.ToString(),
                        MessageId = update.CallbackQuery.Message.Id.ToString(),
                    }).ConfigureAwait(false);
                }

                telegramService.EditMessageAsync(returnedMessage).ConfigureAwait(false);

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

                    telegramService.SendMessageAsync(requestMessage).ConfigureAwait(false);
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
                        
                        telegramService.SendCallbackAnswerAsync(new AnswerCallbackQueryRequest()
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

                        await telegramService.EditMessageAsync(returnedMessage);

                        dbContext.Conversations.Update(conversation);
                        dbContext.Save();
                        return;
                    }

                    if (versionedMr.OwnerId.Equals(needUser.UserId))
                    {
                        await telegramService.SendCallbackAnswerAsync(new AnswerCallbackQueryRequest()
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

                    if (needUser.ChatId.Equals(update.CallbackQuery.Message.Chat.Id.ToString()))
                    {
                        telegramService.RemoveMessageAsync(new RemoveMessageRequest()
                        {
                            ChatId = update.CallbackQuery.Message.Chat.Id.ToString(),
                            MessageId = update.CallbackQuery.Message.Id.ToString(),
                        }).ConfigureAwait(false);
                    }

                    telegramService.EditMessageAsync(returnedMessage).ConfigureAwait(false);

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

                        telegramService.SendMessageAsync(requestMessage).ConfigureAwait(false);
                    }
                }
            }
            
            dbContext.Conversations.Update(conversation);
            dbContext.Save();
        }

        private static async Task SetupMessageReactionAsync(Update update, Telegram telegramService, UnitOfWork dbContext, Action<Guid, ActionType> addAction, string messageId)
        {
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

            var conversations = dbContext.Conversations.GetAll();

            var conversation = conversations.FirstOrDefault(c => c.MRChat != null);

            if (conversation == null) return;

            var needMr = conversation.ListOfMerge.FirstOrDefault(m => m.TelegramMessageId.Equals(messageId));

            var userId = update.CallbackQuery.Sender.Id.ToString();
            var users = dbContext.Users.GetAll();
            var actions = dbContext.Actions.GetAll();
            var needUser = SaveIfNeedUser(update.CallbackQuery.Sender, dbContext);

            //WatchBlock
            {
                var action = actions.FirstOrDefault(a =>
                    a.MessageId == update.CallbackQuery.Message.Id.ToString() && a.ActionFor == needUser.ChatId);

                if (action != null)
                {
                    action.IsActive = false;

                    dbContext.Actions.Update(action);

                    addAction.Invoke(action.Id, ActionType.Remove);
                }
            }

            if (needMr != null)
            {
                if (needMr.Reactions.Any(r => r.ReactionType == ReactionType.Like && r.UserId.Equals(userId)))
                {
                    needMr.Reactions.RemoveAll(r => r.ReactionType == ReactionType.Like && r.UserId.Equals(userId));
                    
                    await telegramService.SendCallbackAnswerAsync(new AnswerCallbackQueryRequest()
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

                    telegramService.EditMessageAsync(returnedMessage).ConfigureAwait(false);

                    dbContext.Conversations.Update(conversation);
                    dbContext.Save();
                    return;
                }

                if (needMr.OwnerId.Equals(needUser.UserId))
                {
                    GetMergeStatAsync(update, telegramService, dbContext, addAction, messageId).ConfigureAwait(false);

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

                if (needUser.ChatId.Equals(update.CallbackQuery.Message.Chat.Id.ToString()))
                {
                    telegramService.RemoveMessageAsync(new RemoveMessageRequest()
                    {
                        ChatId = update.CallbackQuery.Message.Chat.Id.ToString(),
                        MessageId = update.CallbackQuery.Message.Id.ToString(),
                    }).ConfigureAwait(false);
                }

                await telegramService.EditMessageAsync(returnedMessage);

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
                            dbContext.Actions.Update(action);

                            addAction.Invoke(action.Id, ActionType.Remove);
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

                        telegramService.SendMessageAsync(requestMessage).ConfigureAwait(false);
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
                        
                        telegramService.SendCallbackAnswerAsync(new AnswerCallbackQueryRequest()
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

                        telegramService.EditMessageAsync(returnedMessage).ConfigureAwait(false);

                        dbContext.Conversations.Update(conversation);
                        dbContext.Save();
                        return;
                    }

                    if (versionedMr.OwnerId.Equals(needUser.UserId))
                    {
                        GetMergeStatAsync(update, telegramService, dbContext, addAction, messageId).ConfigureAwait(false);
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

                    if (needUser.ChatId.Equals(update.CallbackQuery.Message.Chat.Id.ToString()))
                    {
                        telegramService.RemoveMessageAsync(new RemoveMessageRequest()
                        {
                            ChatId = update.CallbackQuery.Message.Chat.Id.ToString(),
                            MessageId = update.CallbackQuery.Message.Id.ToString(),
                        }).ConfigureAwait(false);
                    }

                    telegramService.EditMessageAsync(returnedMessage).ConfigureAwait(false);

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

                            telegramService.SendMessageAsync(requestMessage).ConfigureAwait(false);
                        }
                    }
                }
            }
            
            dbContext.Conversations.Update(conversation);
            dbContext.Save();
        }

        private static async Task StartWatchMergeAsync(Update update, Telegram telegramService, UnitOfWork dbContext, Action<Guid, ActionType> addAction, string messageId)
        {
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

            var conversations = dbContext.Conversations.GetAll();

            var conversation = conversations.FirstOrDefault(c => c.MRChat != null);

            if (conversation == null) return;

            var needMr = conversation.ListOfMerge.FirstOrDefault(m => m.TelegramMessageId.Equals(messageId));

            var userId = update.CallbackQuery.Sender.Id.ToString();
            var users = dbContext.Users.GetAll();
            var needUser = SaveIfNeedUser(update.CallbackQuery.Sender, dbContext);

            if (needMr != null)
            {
                if (needMr.Reactions.Any(r => r.ReactionType == ReactionType.Watch && r.UserId.Equals(userId)))
                {
                    telegramService.SendCallbackAnswerAsync(new AnswerCallbackQueryRequest()
                    {
                        IsNeedShowAlert = true,
                        Text = "Your couldn't unmarked this MR, please set reaction 👍 or 🚫",
                        CallbackId = update.CallbackQuery.Id,
                    }).ConfigureAwait(false);

                    return;
                }

                if (needMr.OwnerId.Equals(needUser.UserId))
                {
                    GetMergeStatAsync(update, telegramService, dbContext, addAction, messageId).ConfigureAwait(false);

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

                telegramService.EditMessageAsync(returnedMessage).ConfigureAwait(false);
                telegramService.SendCallbackAnswerAsync(new AnswerCallbackQueryRequest()
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

                    telegramService.SendMessageAsync(requestMessage).ConfigureAwait(false);
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
                        telegramService.SendCallbackAnswerAsync(new AnswerCallbackQueryRequest()
                        {
                            IsNeedShowAlert = true,
                            Text = "Your couldn't unmarked this MR, please set reaction 👍 or 🚫",
                            CallbackId = update.CallbackQuery.Id,
                        }).ConfigureAwait(false);

                        return;
                    }

                    if (versionedMr.OwnerId.Equals(needUser.UserId))
                    {
                        GetMergeStatAsync(update, telegramService, dbContext, addAction, messageId).ConfigureAwait(false);
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

                    telegramService.EditMessageAsync(returnedMessage).ConfigureAwait(false);
                    telegramService.SendCallbackAnswerAsync(new AnswerCallbackQueryRequest()
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

                        telegramService.SendMessageAsync(requestMessage).ConfigureAwait(false);
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

            dbContext.Actions.Create(action);
            dbContext.Conversations.Update(conversation);
            dbContext.Save();

            addAction.Invoke(action.Id, ActionType.Add);
        }

        private static void AddOrUpdateUser(User user, UnitOfWork dbContext, bool isNeedUpdate = true)
        {
            var users = dbContext.Users.GetAll();

            if (users.Any(u => u.UserId.Equals(user.UserId)))
            {
                if (isNeedUpdate)
                    dbContext.Users.Update(user);
            }
            else
            {
                dbContext.Users.Create(user);
            }
        }

        private static User SaveIfNeedUser(TelegramBotApi.Models.User user, UnitOfWork dbContext)
        {
            var users = dbContext.Users.GetAll();
            var needUser = users.FirstOrDefault(u => u.UserId.Equals(user.Id.ToString()));
            if (needUser == null)
            {
                var savedUser = new User()
                {
                    Name = user.GetUserFullName(),
                    UserId = user.Id.ToString(),
                };

                AddOrUpdateUser(savedUser, dbContext);

                needUser = savedUser;
            }

            return needUser;
        }
    }
}
