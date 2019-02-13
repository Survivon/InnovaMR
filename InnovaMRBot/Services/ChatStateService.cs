using InnovaMRBot.Models;
using InnovaMRBot.Repository;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using InnovaMRBot.Commands;
using InnovaMRBot.Helpers;
using InnovaMRBot.SubCommand;
using TelegramBotApi.Extension;
using TelegramBotApi.Models;
using TelegramBotApi.Models.Enum;
using TelegramBotApi.Models.Keyboard;
using TelegramBotApi.Models.Keyboard.Interface;
using TelegramBotApi.Telegram;
using TelegramBotApi.Telegram.Request;
using MessageReaction = InnovaMRBot.Models.MessageReaction;
using User = InnovaMRBot.Models.User;

namespace InnovaMRBot.Services
{
    public class ChatStateService
    {
        #region Constants

        private const string MARK_MR_CONVERSATION = "/start MR chat";
        
        private const string REMOVE_MR_CONVERSATION = "/remove MR chat";

        private const string MR_REMOVE_PATTERN = @"https?:\/\/gitlab.fortia.fr\/Fortia\/Innova\/merge_requests\/";
        
        private object _lockerSaveToDbObject = new object();

        private readonly List<BaseCommand> _commands;

        #endregion

        private readonly Telegram _telegramService;

        private readonly UnitOfWork _dbContext;

        public ChatStateService(Telegram telegram, UnitOfWork dbContext)
        {
            _telegramService = telegram;
            _dbContext = dbContext;

            _commands = new List<BaseCommand>()
            {
                new CommonDocumentCommand(_telegramService, _dbContext),
                new HelpCommand(_telegramService, _dbContext),
                new MergeRequestCommand(_telegramService, _dbContext),
                new StartCommand(_telegramService, _dbContext),

                new SprintCommand(_telegramService, _dbContext),
                new SprintAddActionSubCommand(_telegramService, _dbContext),
                new SprintAddDateActionSubCommand(_telegramService, _dbContext),
                new SprintUpdateActionSubCommand(_telegramService, _dbContext),
                new SprintUpdateDateActionSubCommand(_telegramService, _dbContext),
                new SprintRemoveActionSubCommand(_telegramService, _dbContext),

                new GetStatisticCommand(_telegramService, _dbContext),
                new GetStatisticAllActionSubCommand(_telegramService, _dbContext),
                new GetStatisticSprintActionSubCommand(_telegramService, _dbContext),
                new GetStatisticDateActionSubCommand(_telegramService, _dbContext),

                new EditCommand(_telegramService, _dbContext),
                new EditMergeNumberActionSubCommand(_telegramService, _dbContext),
            };
        }

        public async Task GetUpdateFromTelegramAsync(Update update)
        {
            if (update.Message != null)
            {
                var message = update.Message.Text;
                var userId = update.Message.Sender.Id.ToString();

                var user = _dbContext.Users.GetAll().FirstOrDefault(u => u.UserId.Equals(userId));

                if (user != null && user.Commands.Any())
                {
                    var lastCommand = user.Commands.LastOrDefault();

                    var command = _commands.FirstOrDefault(c => c.IsThisCommand(lastCommand.Command));
                    command?.WorkOnAnswerAsync(update).ConfigureAwait(false);
                }
                else
                {
                    var command = _commands.FirstOrDefault(c => c.IsThisCommand(message));
                    command?.WorkerAsync(update).ConfigureAwait(false);
                }
            }
            else if (update.ChanelMessage != null)
            {
                // for work with chanel messages
                var message = update.ChanelMessage.Text;
                var answerMessages = new List<SendMessageRequest>();

                if (message.Equals(MARK_MR_CONVERSATION))
                {
                    answerMessages.Add(SetupMRConversation(update));
                }
                else if (message.Equals(REMOVE_MR_CONVERSATION))
                {
                    answerMessages.Add(await RemoveMrConversationAsync(update));
                }

                foreach (var sendMessageRequest in answerMessages)
                {
                    if (string.IsNullOrEmpty(sendMessageRequest.Text)) continue;

                    _telegramService.SendMessageAsync(sendMessageRequest).ConfigureAwait(false);
                }
            }
            else if (update.CallbackQuery != null)
            {
                switch (update.CallbackQuery.Data)
                {
                    case "/success reaction":
                        SetupMessageReactionAsync(update).ConfigureAwait(false);
                        break;
                    case "/get stat":
                        GetMergeStatAsync(update).ConfigureAwait(false);
                        break;
                    case "/bad reaction":
                        SetupBadReaction(update).ConfigureAwait(false);
                        break;
                }
            }
            else if (update.InlineQuery != null)
            {

            }
            else if (update.InlineResult != null)
            {

            }

            lock (_lockerSaveToDbObject)
            {
                _dbContext.Save();
            }
        }

        #region Telegram part
        
        private async Task GetMergeStatAsync(Update update)
        {
            var messageId = update.CallbackQuery.Message.Id.ToString();

            var conversations = _dbContext.Conversations.GetAll();
            var conversation = conversations.FirstOrDefault(c => c.MRChat != null);

            if (conversation == null) return;

            var users = _dbContext.Users.GetAll().ToList();
            var currentUser = SaveIfNeedUser(update.CallbackQuery.Sender);

            var needMr = conversation.ListOfMerge.FirstOrDefault(m => m.TelegramMessageId.Equals(messageId));
            if (needMr != null)
            {
                var textForShare = new StringBuilder();
                textForShare.AppendLine($"Reaction for MR {new Regex(MR_REMOVE_PATTERN).Replace(needMr.MrUrl, string.Empty)} by {users.FirstOrDefault(c => c.UserId.Equals(needMr.OwnerId)).Name}");
                textForShare.AppendLine();

                if (needMr.Reactions.Any(r => r.ReactionType == ReactionType.Like))
                {
                    textForShare.AppendLine("Members:");
                    foreach (var messageReaction in needMr.Reactions.Where(r => r.ReactionType == ReactionType.Like))
                    {
                        textForShare.AppendLine(
                            $"{users.FirstOrDefault(c => c.UserId.Equals(messageReaction.UserId)).Name} in {messageReaction.ReactionTime.GetUserTime(currentUser)}");
                    }
                }
                else
                {
                    textForShare.AppendLine("No reactions to this MR 😔");
                }

                await _telegramService.SendCallbackAnswerAsync(new AnswerCallbackQueryRequest()
                {
                    IsNeedShowAlert = true,
                    Text = textForShare.ToString(),
                    CallbackId = update.CallbackQuery.Id,
                });
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

                    if (versionOffMr.Reactions.Any(r => r.ReactionType == ReactionType.Like))
                    {
                        textForShare.AppendLine("Members:");
                        foreach (var messageReaction in versionOffMr.Reactions.Where(r => r.ReactionType == ReactionType.Like))
                        {
                            textForShare.AppendLine(
                                $"{users.FirstOrDefault(c => c.UserId.Equals(messageReaction.UserId)).Name} in {messageReaction.ReactionTime.GetUserTime(currentUser)}");
                        }
                    }
                    else
                    {
                        textForShare.AppendLine("No reactions to this MR 😔");
                    }

                    await _telegramService.SendCallbackAnswerAsync(new AnswerCallbackQueryRequest()
                    {
                        IsNeedShowAlert = true,
                        Text = textForShare.ToString(),
                        CallbackId = update.CallbackQuery.Id,
                    });
                }
            }
        }

        private async Task SetupBadReaction(Update update)
        {
            var conversationId = update.CallbackQuery.Message.Chat.Id.ToString();

            var returnedMessage = new EditMessageTextRequest()
            {
                ChatId = conversationId,
                FormattingMessageType = FormattingMessageType.Default,
            };

            var messageId = update.CallbackQuery.Message.Id.ToString();

            var conversations = _dbContext.Conversations.GetAll();

            var conversation = conversations.FirstOrDefault(c => c.MRChat != null);

            if (conversation == null) return;

            var needMr = conversation.ListOfMerge.FirstOrDefault(m => m.TelegramMessageId.Equals(messageId));

            var userId = update.CallbackQuery.Sender.Id.ToString();
            var users = _dbContext.Users.GetAll();
            var needUser = SaveIfNeedUser(update.CallbackQuery.Sender);

            if (needMr != null)
            {
                if (needMr.Reactions.Any(r => r.ReactionType == ReactionType.DisLike && r.UserId.Equals(userId)))
                {
                    needMr.Reactions.RemoveAll(r => r.ReactionType == ReactionType.DisLike && r.UserId.Equals(userId));

                    _dbContext.Conversations.Update(conversation);

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
                        needMr.Reactions.Count(c => c.ReactionType == ReactionType.DisLike));

                    returnedMessage.ChatId = conversation.MRChat.Id;

                    returnedMessage.Text = $"{needMr.Description} \nby {users.FirstOrDefault(c => c.UserId.Equals(needMr.OwnerId)).Name}";
                    returnedMessage.EditMessageId = messageId;

                    _telegramService.EditMessageAsync(returnedMessage).ConfigureAwait(false);

                    lock (_lockerSaveToDbObject)
                    {
                        _dbContext.Save();
                    }

                    return;
                }

                if (needMr.OwnerId.Equals(needUser.UserId))
                {
                    await _telegramService.SendCallbackAnswerAsync(new AnswerCallbackQueryRequest()
                    {
                        IsNeedShowAlert = true,
                        Text = "You couldn't mark your MR 😊",
                        CallbackId = update.CallbackQuery.Id,
                    });

                    return;
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
                    needMr.Reactions.Count(c => c.ReactionType == ReactionType.DisLike));

                returnedMessage.ChatId = conversation.MRChat.Id;
                returnedMessage.Text = $"{needMr.Description} \nby {users.FirstOrDefault(c => c.UserId.Equals(needMr.OwnerId)).Name}";
                returnedMessage.EditMessageId = messageId;

                _telegramService.EditMessageAsync(returnedMessage).ConfigureAwait(false);
                _dbContext.Conversations.Update(conversation);

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

                        _dbContext.Conversations.Update(conversation);

                        await _telegramService.SendCallbackAnswerAsync(new AnswerCallbackQueryRequest()
                        {
                            IsNeedShowAlert = true,
                            Text = "Your reaction has been cancelled",
                            CallbackId = update.CallbackQuery.Id,
                        });

                        returnedMessage.AddButtonForRequest(
                            versionedMr.MrUrl,
                            versionedMr.TicketsUrl.Split(';').ToList(),
                            versionOffMr.Reactions.Count(c => c.ReactionType == ReactionType.Like),
                            versionOffMr.Reactions.Count(c => c.ReactionType == ReactionType.DisLike));

                        returnedMessage.ChatId = conversation.MRChat.Id;
                        returnedMessage.Text = $"{versionOffMr.Description} \nby {users.FirstOrDefault(c => c.UserId.Equals(versionedMr.OwnerId)).Name}";
                        returnedMessage.EditMessageId = messageId;

                        await _telegramService.EditMessageAsync(returnedMessage);

                        lock (_lockerSaveToDbObject)
                        {
                            _dbContext.Save();
                        }

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
                        versionOffMr.Reactions.Count(c => c.ReactionType == ReactionType.DisLike));

                    returnedMessage.ChatId = conversation.MRChat.Id;
                    returnedMessage.Text = $"{versionOffMr.Description} \nby {users.FirstOrDefault(c => c.UserId.Equals(versionedMr.OwnerId)).Name}";
                    returnedMessage.EditMessageId = messageId;

                    await _telegramService.EditMessageAsync(returnedMessage);
                    _dbContext.Conversations.Update(conversation);


                    if (versionedMr.Owner == null)
                    {
                        versionedMr.Owner = users.FirstOrDefault(u => u.UserId.Equals(versionedMr.OwnerId));
                    }

                    var chatWithMrsUser = needMr.Owner.ChatId;
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

            await _telegramService.SendCallbackAnswerAsync(new AnswerCallbackQueryRequest()
            {
                CallbackId = update.CallbackQuery.Id,
            });

            lock (_lockerSaveToDbObject)
            {
                _dbContext.Save();
            }

        }

        private async Task SetupMessageReactionAsync(Update update)
        {
            var conversationId = update.CallbackQuery.Message.Chat.Id.ToString();

            var returnedMessage = new EditMessageTextRequest()
            {
                ChatId = conversationId,
                FormattingMessageType = FormattingMessageType.Default,
            };

            var messageId = update.CallbackQuery.Message.Id.ToString();

            var conversations = _dbContext.Conversations.GetAll();

            var conversation = conversations.FirstOrDefault(c => c.MRChat != null);

            if (conversation == null) return;

            var needMr = conversation.ListOfMerge.FirstOrDefault(m => m.TelegramMessageId.Equals(messageId));

            var userId = update.CallbackQuery.Sender.Id.ToString();
            var users = _dbContext.Users.GetAll();
            var needUser = SaveIfNeedUser(update.CallbackQuery.Sender);

            if (needMr != null)
            {
                if (needMr.Reactions.Any(r => r.ReactionType == ReactionType.Like && r.UserId.Equals(userId)))
                {
                    needMr.Reactions.RemoveAll(r => r.ReactionType == ReactionType.Like && r.UserId.Equals(userId));

                    _dbContext.Conversations.Update(conversation);

                    await _telegramService.SendCallbackAnswerAsync(new AnswerCallbackQueryRequest()
                    {
                        IsNeedShowAlert = true,
                        Text = "Your reaction has been cancelled",
                        CallbackId = update.CallbackQuery.Id,
                    });

                    returnedMessage.AddButtonForRequest(
                        needMr.MrUrl,
                        needMr.TicketsUrl.Split(';').ToList(),
                        needMr.Reactions.Count(c => c.ReactionType == ReactionType.Like),
                        needMr.Reactions.Count(c => c.ReactionType == ReactionType.DisLike));

                    returnedMessage.ChatId = conversation.MRChat.Id;
                    returnedMessage.Text = $"{needMr.Description} \nby {users.FirstOrDefault(c => c.UserId.Equals(needMr.OwnerId)).Name}";
                    returnedMessage.EditMessageId = messageId;

                    await _telegramService.EditMessageAsync(returnedMessage).ConfigureAwait(false);

                    lock (_lockerSaveToDbObject)
                    {
                        _dbContext.Save();
                    }

                    return;
                }

                if (needMr.OwnerId.Equals(needUser.UserId))
                {
                    GetMergeStatAsync(update).ConfigureAwait(false);

                    return;
                }

                var reaction = new MessageReaction()
                {
                    ReactionTime = DateTimeOffset.UtcNow,
                    UserId = needUser.UserId,
                    ReactionType = ReactionType.Like,
                };

                reaction.SetReactionInMinutes(needMr.PublishDate.Value);

                needMr.Reactions.Add(reaction);

                needMr.Reactions.RemoveAll(r => r.ReactionType == ReactionType.DisLike && r.UserId == needUser.UserId);

                returnedMessage.AddButtonForRequest(
                    needMr.MrUrl,
                    needMr.TicketsUrl.Split(';').ToList(),
                    needMr.Reactions.Count(c => c.ReactionType == ReactionType.Like),
                    needMr.Reactions.Count(c => c.ReactionType == ReactionType.DisLike));

                returnedMessage.ChatId = conversation.MRChat.Id;
                returnedMessage.Text = $"{needMr.Description} \nby {users.FirstOrDefault(c => c.UserId.Equals(needMr.OwnerId)).Name}";
                returnedMessage.EditMessageId = messageId;

                await _telegramService.EditMessageAsync(returnedMessage);
                _dbContext.Conversations.Update(conversation);

                if (needMr.Reactions.Count(r => r.ReactionType == ReactionType.Like) == 2)
                {
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

                        _dbContext.Conversations.Update(conversation);

                        await _telegramService.SendCallbackAnswerAsync(new AnswerCallbackQueryRequest()
                        {
                            IsNeedShowAlert = true,
                            Text = "Your reaction has been cancelled",
                            CallbackId = update.CallbackQuery.Id,
                        });

                        versionOffMr.Reactions.RemoveAll(r => r.ReactionType == ReactionType.DisLike && r.UserId == needUser.UserId);

                        returnedMessage.AddButtonForRequest(
                            versionedMr.MrUrl,
                            versionedMr.TicketsUrl.Split(';').ToList(),
                            versionOffMr.Reactions.Count(c => c.ReactionType == ReactionType.Like),
                            versionOffMr.Reactions.Count(c => c.ReactionType == ReactionType.DisLike));

                        returnedMessage.ChatId = conversation.MRChat.Id;
                        returnedMessage.Text = $"{versionOffMr.Description} \nby {users.FirstOrDefault(c => c.UserId.Equals(versionedMr.OwnerId)).Name}";
                        returnedMessage.EditMessageId = messageId;

                        await _telegramService.EditMessageAsync(returnedMessage);

                        lock (_lockerSaveToDbObject)
                        {
                            _dbContext.Save();
                        }

                        return;
                    }

                    if (versionedMr.OwnerId.Equals(needUser.UserId))
                    {
                        GetMergeStatAsync(update).ConfigureAwait(false);
                        return;
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
                        versionOffMr.Reactions.Count(c => c.ReactionType == ReactionType.DisLike));

                    returnedMessage.ChatId = conversation.MRChat.Id;
                    returnedMessage.Text = $"{versionOffMr.Description} \nby {users.FirstOrDefault(c => c.UserId.Equals(versionedMr.OwnerId)).Name}";
                    returnedMessage.EditMessageId = messageId;

                    await _telegramService.EditMessageAsync(returnedMessage);
                    _dbContext.Conversations.Update(conversation);

                    if (versionOffMr.Reactions.Count(r => r.ReactionType == ReactionType.Like) == 2)
                    {
                        if (versionedMr.Owner == null)
                        {
                            versionedMr.Owner = users.FirstOrDefault(u => u.UserId.Equals(needMr.OwnerId));
                        }

                        var chatWithMrsUser = needMr.Owner.ChatId;
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

            await _telegramService.SendCallbackAnswerAsync(new AnswerCallbackQueryRequest()
            {
                CallbackId = update.CallbackQuery.Id,
            });

            lock (_lockerSaveToDbObject)
            {
                _dbContext.Save();
            }
        }

        private SendMessageRequest SetupMRConversation(Update message)
        {
            var conversationId = message.ChanelMessage.Chat.Id.ToString();

            var resultMessage = new SendMessageRequest()
            {
                ChatId = conversationId,
            };

            var conversations = _dbContext.Conversations.GetAll();

            if (conversations == null || !conversations.Any())
            {
                var syncId = Guid.NewGuid();
                var chatSetting = new ChatSetting()
                {
                    Id = conversationId,
                    IsMRChat = true,
                    SyncId = syncId,
                    Name = message.ChanelMessage.Chat.Title,
                };

                var newConversation = new ConversationSetting()
                {
                    MRChat = chatSetting,
                };

                _dbContext.Conversations.Create(newConversation);

                resultMessage.Text = $"Current chat is setup as MR with sync id: {syncId}";
            }
            else
            {
                if (!conversations.Any(c => c.MRChat != null && c.MRChat.Id.Equals(conversationId)))
                {
                    var syncId = Guid.NewGuid();
                    var chatSetting = new ChatSetting()
                    {
                        Id = conversationId,
                        IsMRChat = true,
                        SyncId = syncId,
                        Name = message.ChanelMessage.Chat.Title,
                    };

                    var newConversation = new ConversationSetting()
                    {
                        MRChat = chatSetting,
                    };

                    _dbContext.Conversations.Create(newConversation);

                    resultMessage.Text = $"Current chat is setup as MR with sync id: {syncId}";
                }
            }

            return resultMessage;
        }

        private async Task<SendMessageRequest> RemoveMrConversationAsync(Update message)
        {
            var convesationId = message.ChanelMessage.Chat.Id.ToString();
            var responseMessage = new SendMessageRequest()
            {
                ChatId = convesationId,
            };

            SaveIfNeedUser(message.ChanelMessage.ForwardSender);

            var conversations = _dbContext.Conversations.GetAll();

            var needConversation = conversations.FirstOrDefault(c => c.MRChat.Id.Equals(convesationId));
            if (needConversation == null)
            {
                responseMessage.Text =
                    "This is not a MR's conversation or you don't add any conversation. Try in MR's conversation ;)";
            }
            else
            {
                //if (needConversation.Admins.Any(u => u.UserId.Equals(userId)))
                //{
                //    responseMessage.Text = "Congratulation, you remove all data with linked for current conversation";
                //    // TODO: add get stat and send before remove
                //    await RemoveConversationAsync(needConversation);
                //}
                //else
                //{
                //    responseMessage.Text = "You don't have permission for remove this conversation!";
                //}
            }

            return responseMessage;
        }

        #endregion

        #region Helpers

        private void AddOrUpdateUser(User user, bool isNeedUpdate = true)
        {
            var users = _dbContext.Users.GetAll();

            if (users.Any(u => u.UserId.Equals(user.UserId)))
            {
                if (isNeedUpdate)
                    _dbContext.Users.Update(user);
            }
            else
            {
                _dbContext.Users.Create(user);
            }
        }

        private string GetUserFullName(TelegramBotApi.Models.User user)
        {
            return $"{user.FirstName} {user.LastName}";
        }

        private User SaveIfNeedUser(TelegramBotApi.Models.User user)
        {
            var users = _dbContext.Users.GetAll();
            var needUser = users.FirstOrDefault(u => u.UserId.Equals(user.Id.ToString()));
            if (needUser == null)
            {
                var savedUser = new User()
                {
                    Name = GetUserFullName(user),
                    UserId = user.Id.ToString(),
                };

                AddOrUpdateUser(savedUser);

                needUser = savedUser;
            }

            return needUser;
        }
        
        #endregion
    }
}
