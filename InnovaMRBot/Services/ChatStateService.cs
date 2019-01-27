using InnovaMRBot.Models;
using InnovaMRBot.Repository;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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

        private const string MARK_ALERT_CONVERSATION = "/start alert chat";

        private const string REMOVE_MR_CONVERSATION = "/remove MR chat";

        private const string REMOVE_ALERT_CONVERSATION = "/remove alert chat";

        private const string SETUP_ADMIN = "/setup admin";

        private const string REMOVE_ADMIN = "/remove admin";

        private const string GET_STATISTIC = "/get_stat";

        private const string GET_UNMARKED_MR = "/get MR";

        private const string GET_MY_UNMARKED_MR = "/get my MR";

        private const string START_READ_ALL_MESSAGE = "/get all message";

        private const string MR_PATTERN = @"https?:\/\/gitlab.fortia.fr\/Fortia\/Innova\/merge_requests\/[0-9]+";

        private const string MR_WITH_DIFF_PATTERN = @"https?:\/\/gitlab.fortia.fr\/Fortia\/Innova\/merge_requests\/[0-9]+\/diffs";

        private const string MR_WITH_SLASH_PATTERN = @"https?:\/\/gitlab.fortia.fr\/Fortia\/Innova\/merge_requests\/[0-9]+\/";

        private const string MR_WITH_COMMITS_PATTERN = @"https?:\/\/gitlab.fortia.fr\/Fortia\/Innova\/merge_requests\/[0-9]+\/commits";

        private const string MR_REMOVE_PATTERN = @"https?:\/\/gitlab.fortia.fr\/Fortia\/Innova\/merge_requests\/";

        private const string TICKET_PATTERN = @"https?:\/\/fortia.atlassian.net\/browse\/\w+-[0-9]+";

        private const string CONVERSATION_KEY = "conversation";

        private const string USER_SETTING_KEY = "users";

        private const string GUID_PATTERN = @"(\{){0,1}[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}(\}){0,1}";

        private const string TICKET_NUMBER_PATTERN = @"\w+-[0-9]+";

        private const int MAX_COUNT_OF_ADMINS = 2;

        private const string START = @"/start";

        private const string HELP = @"/help";

        private const string COMMON_DOCUMENT = @"/getcommondocument";

        private const string SPRINT_WORKER = @"/sprint";

        private object _lockerSaveToDbObject = new object();

        private readonly List<string> _changesNotation = new List<string>()
        {
            "UPDATED",
            "ИЗМЕНЕНО"
        };

        #endregion

        private readonly Telegram _telegramService;

        private readonly UnitOfWork _dbContext;

        public ChatStateService(Telegram telegram, UnitOfWork dbContext)
        {
            _telegramService = telegram;
            _dbContext = dbContext;
        }

        public async Task GetUpdateFromTelegramAsync(Update update)
        {
            if (update.Message != null)
            {
                var answerMessages = new List<SendMessageRequest>();
                var message = update.Message.Text;
                if (message.Equals(START))
                {
                    var savedUser = new User()
                    {
                        Name = GetUserFullName(update.Message.Sender),
                        UserId = update.Message.Sender.Id.ToString(),
                    };

                    AddOrUpdateUser(savedUser, false);

                    // get start message
                    answerMessages.Add(new SendMessageRequest()
                    {
                        ChatId = update.Message.Chat.Id.ToString(),
                        Text = $"Hi, {savedUser.Name}! I'm Bot for help to work with MR for Innova 😊 If you have some question please send me /help or visit site http://innovamrbot.azurewebsites.net/",
                    });
                }
                else if (message.Equals(HELP))
                {
                    answerMessages.Add(new SendMessageRequest()
                    {
                        Text = @"<b>How to send MR?</b>
1.Write you message with <i>MR Link</i>, <i>Ticket Link</i> and <i>Description</i>
2.If everything is correct Bot send it to chanel with other MRs
<b>How to get statistics from MRs?</b>
<i>/get stat getalldata</i> command for get all data about MR(links, publish date, reviewers, etc.)
<i>/get stat getmrreaction</i> command for get reaction on ticket
<i>/get stat getusermrreaction</i> command for get user reaction on tickets
<i>/get stat getunmarked</i> command for get count of unmarked MR per days
For all of this statistics you can add start and end date of publish date(For ex. <b>/get stat getalldata 24/11/2018 28/11/2018</b>)
🚫 - mark MR that it has some conflicts or bad code, after mark please send message to MRs owner",
                        ChatId = update.Message.Chat.Id.ToString(),
                        FormattingMessageType = FormattingMessageType.HTML,
                    });

                }
                else if (message.Equals(COMMON_DOCUMENT))
                {
                    answerMessages.Add(new SendMessageRequest()
                    {
                        Text = "Document Link",
                        ChatId = update.Message.Chat.Id.ToString(),
                        ReplyMarkup = new InlineKeyboardMarkup()
                        {
                            InlineKeyboardButtons = new List<List<InlineKeyboardButton>>()
                            {
                                new List<InlineKeyboardButton>()
                                {
                                    new InlineKeyboardButton()
                                    {
                                        Text = "Link",
                                        Url = "https://docs.google.com/document/d/1MNI8ZY-Fciqk6q7PZnJz2aDQe4TllQHsdOo6jpim_9s/edit",
                                    },
                                },
                            },
                        },
                    });
                }
                else if (message.StartsWith(GET_STATISTIC, StringComparison.InvariantCultureIgnoreCase))
                {
                    // get statistic
                    GetStatisticsAsync(update).ConfigureAwait(false);
                }
                else if (message.StartsWith(SPRINT_WORKER, StringComparison.InvariantCultureIgnoreCase))
                {
                    SprintWorkAsync(update).ConfigureAwait(false);
                }
                else
                {
                    // for other message / get MR
                    GetMRMessageAsync(update).ConfigureAwait(false);
                }

                foreach (var sendMessageRequest in answerMessages)
                {
                    await _telegramService.SendMessageAsync(sendMessageRequest);
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

        private async Task SprintWorkAsync(Update update)
        {
            var message = update.Message.Text;
            var responseMessage = string.Empty;

            message = message.Replace(SPRINT_WORKER, string.Empty).Trim(' ');

            var keywords = message.Split(' ');

            if (keywords.Length > 1)
            {
                var conversations = _dbContext.Conversations.GetAll();
                var conversation = conversations.FirstOrDefault(c => c.MRChat != null);

                var sprints = conversation.MRChat.Sprints;
                var number = Convert.ToInt32(keywords[1]);

                switch (keywords[0])
                {
                    case "add":
                        if (sprints == null) sprints = new List<Sprint>();
                        
                        if (keywords.Length > 3)
                        {
                            try
                            {
                                var start = ConvertToDate(keywords[2]);
                                var end = ConvertToDate(keywords[3]);

                                sprints.Add(new Sprint()
                                {
                                    End = end,
                                    Number = number,
                                    Start = start,
                                });
                                responseMessage = "Sprint save!";
                                _dbContext.Conversations.Update(conversation);
                            }
                            catch (Exception e)
                            {
                                responseMessage = "Incorrect date input try format M/d/yyyy";
                            }
                        }
                        else
                        {
                            responseMessage = "Need add date info start than end";
                        }

                        break;
                    case "update":
                        if (sprints.Any())
                        {
                            var sprint = sprints.FirstOrDefault(s => s.Number == number);

                            if (sprint != null)
                            {
                                try
                                {
                                    var start = ConvertToDate(keywords[2]);
                                    var end = ConvertToDate(keywords[3]);
                                    sprint.End = end;
                                    sprint.Start = start;
                                    responseMessage = "You successfuly update sprint info";
                                    _dbContext.Conversations.Update(conversation);
                                }
                                catch (Exception e)
                                {
                                    responseMessage = "Incorrect date input try format M/d/yyyy";
                                }
                            }
                            else
                            {
                                responseMessage = $"Sprint number {number} doesn't exist";
                            }
                        }
                        else
                        {
                            responseMessage = "You don't have any message";
                        }

                        break;
                    case "delete":
                        if (sprints.Any())
                        {
                            var sprint = sprints.FirstOrDefault(s => s.Number == number);

                            if (sprint != null)
                            {
                                sprints.Remove(sprint);
                                _dbContext.Conversations.Update(conversation);
                            }
                        }
                        else
                        {
                            responseMessage = "You don't have any sprints";
                        }
                        break;
                }
            }
            else
            {
                responseMessage =
                    "Need to add _`command(add, update, delete)` `sprint number` `start date` `end date`_";
            }


            var requestMessage = new SendMessageRequest()
            {
                ChatId = update.Message.Chat.Id.ToString(),
                Text = responseMessage,
                FormattingMessageType = FormattingMessageType.Markdown,
            };

            _telegramService.SendMessageAsync(requestMessage).ConfigureAwait(false);

            _dbContext.Save();
        }

        private async Task GetMergeStatAsync(Update update)
        {
            var messageId = update.CallbackQuery.Message.Id.ToString();

            var conversations = _dbContext.Conversations.GetAll();
            var conversation = conversations.FirstOrDefault(c => c.MRChat != null);

            if (conversation == null) return;

            var users = _dbContext.Users.GetAll().ToList();
            SaveIfNeedUser(update.CallbackQuery.Sender);

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
                            $"{users.FirstOrDefault(c => c.UserId.Equals(messageReaction.UserId)).Name} in {messageReaction.ReactionTime:MM/dd/yyyy H:mm:ss}");
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
                                $"{users.FirstOrDefault(c => c.UserId.Equals(messageReaction.UserId)).Name} in {messageReaction.ReactionTime:MM/dd/yyyy H:mm:ss}");
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

                    AddButtonForRequest(
                        returnedMessage,
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

                AddButtonForRequest(
                    returnedMessage,
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

                        AddButtonForRequest(
                            returnedMessage,
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

                    AddButtonForRequest(
                        returnedMessage,
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

                    AddButtonForRequest(
                        returnedMessage,
                        needMr.MrUrl,
                        needMr.TicketsUrl.Split(';').ToList(),
                        needMr.Reactions.Count(c => c.ReactionType == ReactionType.Like),
                        needMr.Reactions.Count(c => c.ReactionType == ReactionType.DisLike));

                    returnedMessage.ChatId = conversation.MRChat.Id;
                    returnedMessage.Text = $"{needMr.Description} \nby {users.FirstOrDefault(c => c.UserId.Equals(needMr.OwnerId)).Name}";
                    returnedMessage.EditMessageId = messageId;

                    await _telegramService.EditMessageAsync(returnedMessage);

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

                AddButtonForRequest(
                    returnedMessage,
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

                        AddButtonForRequest(
                            returnedMessage,
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

                    AddButtonForRequest(
                        returnedMessage,
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

        private async Task GetMRMessageAsync(Update message)
        {
            var convesationId = message.Message.Chat.Id.ToString();

            var responseMessage = new SendMessageRequest()
            {
                ChatId = convesationId,
            };

            var responseMessageForUser = new SendMessageRequest()
            {
                ChatId = convesationId,
            };

            var messageText = message.Message.Text;

            var mrRegex = new Regex(MR_PATTERN);
            var mrUrlMatch = mrRegex.Match(messageText);

            if (new Regex(MR_WITH_DIFF_PATTERN).IsMatch(messageText))
            {
                messageText = new Regex(MR_WITH_DIFF_PATTERN).Replace(messageText, string.Empty).Trim(new char[] { '\r', '\n' });
            }
            else if (new Regex(MR_WITH_COMMITS_PATTERN).IsMatch(messageText))
            {
                messageText = new Regex(MR_WITH_COMMITS_PATTERN).Replace(messageText, string.Empty).Trim(new char[] { '\r', '\n' });
            }
            else if (new Regex(MR_WITH_SLASH_PATTERN).IsMatch(messageText))
            {
                messageText = new Regex(MR_WITH_SLASH_PATTERN).Replace(messageText, string.Empty).Trim(new char[] { '\r', '\n' });
            }

            var mrUrl = mrUrlMatch.Value;

            var conversations = _dbContext.Conversations.GetAll();

            var conversation = conversations.FirstOrDefault(c => c.MRChat != null);

            if (conversation == null) return;

            if (string.IsNullOrEmpty(mrUrl))
            {
                responseMessageForUser.Text = "Please add MR link to message, thanks 😊";
                await _telegramService.SendMessageAsync(responseMessageForUser);
                return;
            }

            var needUser = SaveIfNeedUser(message.Message.Sender);

            if (await IsMrContainceAsync(mrUrl, conversation.MRChat.Id))
            {
                // for updatedTicket
                var needMr = conversation.ListOfMerge.FirstOrDefault(m => m.MrUrl.Equals(mrUrl));
                if (needMr == null)
                {
                    responseMessageForUser.Text = "I can not to find any MR for update";
                    await _telegramService.SendMessageAsync(responseMessageForUser);
                    return;
                }

                needMr.IsHadAlreadyChange = true;
                needMr.CountOfChange++;

                var description = new Regex(TICKET_PATTERN)
                    .Replace(new Regex(MR_PATTERN).Replace(messageText, string.Empty), string.Empty).Trim();

                var lineOfMessage = description.Split('\n').ToList();
                var firstLine = lineOfMessage.FirstOrDefault();

                if (_changesNotation.Any(c => c.Equals(firstLine, StringComparison.InvariantCultureIgnoreCase)))
                {
                    lineOfMessage.RemoveAt(0);
                    description = string.Join('\r', lineOfMessage);
                }

                if (string.IsNullOrEmpty(description))
                {
                    responseMessageForUser.Text = "Please add description for your MR to message, thanks 😊";
                    await _telegramService.SendMessageAsync(responseMessageForUser);
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
                responseMessage.Text = $"{description} \nby {needUser.Name}";
                AddButtonForRequest(responseMessage, mrUrl, needMr.TicketsUrl.Split(';').ToList());

                var resMessage = await _telegramService.SendMessageAsync(responseMessage);
                versionedTicket.Id = resMessage.Id.ToString();
                responseMessageForUser.Text = "Well done! I'll send it 😊";

                _telegramService.SendMessageAsync(responseMessageForUser).ConfigureAwait(false);


                needMr.VersionedSetting.Add(versionedTicket);

                _dbContext.Conversations.Update(conversation);
            }
            else
            {
                var mrMessage = new MergeSetting { MrUrl = mrUrl, AllText = messageText };

                var ticketRegex = new Regex(TICKET_PATTERN);

                var ticketMatches = ticketRegex.Matches(messageText);

                if (ticketMatches.Count <= 0)
                {
                    responseMessageForUser.Text = "Please add ticket link to message, thanks 😊";
                    await _telegramService.SendMessageAsync(responseMessageForUser);
                    return;
                }

                var description = messageText.Replace(mrUrl, string.Empty);

                foreach (Match ticketMatch in ticketMatches)
                {
                    mrMessage.TicketsUrl += ticketMatch.Value + ";";
                    description = description.Replace(ticketMatch.Value, string.Empty);
                }

                mrMessage.PublishDate = DateTimeOffset.UtcNow;

                var lineOfMessage = description.Split('\r').ToList();
                var firstLine = lineOfMessage.FirstOrDefault();

                if (_changesNotation.Any(c => c.Equals(firstLine, StringComparison.InvariantCultureIgnoreCase)))
                {
                    lineOfMessage.RemoveAt(0);
                    description = string.Join('\r', lineOfMessage);
                }

                if (string.IsNullOrEmpty(description))
                {
                    responseMessageForUser.Text = "Please add description for your MR to message, thanks 😊";
                    await _telegramService.SendMessageAsync(responseMessageForUser);
                    return;
                }

                mrMessage.Description = description.Trim(new char[] { '\r', '\n' });
                mrMessage.OwnerId = needUser.UserId;

                responseMessageForUser.Text = "Well done! I'll send it 😊";

                _telegramService.SendMessageAsync(responseMessageForUser).ConfigureAwait(false);

                AddButtonForRequest(responseMessage, mrUrl, mrMessage.TicketsUrl.Split(';').ToList());

                responseMessage.ChatId = conversation.MRChat.Id;
                responseMessage.Text = $"{description.Trim(new char[] { '\r', '\n' })} \nby {needUser.Name}";

                var resMessage = await _telegramService.SendMessageAsync(responseMessage);

                mrMessage.TelegramMessageId = resMessage.Id.ToString();

                conversation.ListOfMerge.Add(mrMessage);

                _dbContext.Conversations.Update(conversation);
            }

            lock (_lockerSaveToDbObject)
            {
                _dbContext.Save();
            }
        }

        private async Task<SendMessageRequest> SetupUsersForConversationAsync(Update message)
        {
            var convesationId = message.Message.Chat.Id.ToString();
            var userId = message.Message.ForwardSender.Id.ToString();
            var responseMessage = new SendMessageRequest()
            {
                ChatId = convesationId,
            };

            var conversation = _dbContext.Conversations.GetAll().FirstOrDefault(c => c.MRChat.Id == convesationId);

            if (conversation != null)
            {
                if (!conversation.Partisipants.Any(p => p.UserId.Equals(userId)))
                {
                    var newUser = new User()
                    {
                        Name = GetUserFullName(message.Message.ForwardSender),
                        UserId = userId,
                    };

                    conversation.Partisipants.Add(newUser);

                    _dbContext.Conversations.Update(conversation);
                    AddOrUpdateUser(newUser);
                }
            }
            else
            {
                responseMessage.Text = "This conversation is not MR chat!";
            }

            return responseMessage;
        }

        private async Task GetStatisticsAsync(Update update)
        {
            var message = update.Message.Text;
            message = message.Replace(GET_STATISTIC, string.Empty).Trim();
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

            if (conversation != null)
            {
                switch (command)
                {
                    case "_getalldata":
                        result = StatHtmlBuilder.GetAllData(conversation.ListOfMerge.ToList(), users, startDate, endDate);
                        break;
                    case "_getmrreaction":
                        result = StatHtmlBuilder.GetMRReaction(conversation.ListOfMerge.ToList(), users, startDate, endDate);
                        break;
                    case "_getusermrreaction":
                        result = StatHtmlBuilder.GetUsersMRReaction(conversation.ListOfMerge.ToList(), users, startDate, endDate);
                        break;
                    case "_getunmarked":
                        result = StatHtmlBuilder.GetUnmarkedCountMergePerDay(conversation.ListOfMerge.ToList(), users, startDate, endDate);
                        break;
                    case "_getunmarkedperuser":
                        result = StatHtmlBuilder.GetUnmarkedMergePerUser(conversation.ListOfMerge.ToList(), users,
                            startDate, endDate);
                        break;
                        
                }

                var responseMessageForUser = new SendDocumentRequest()
                {
                    ChatId = update.Message.Chat.Id.ToString(),
                    Document = string.Empty
                };

                _telegramService.SendDocumentAsync(responseMessageForUser, result).ConfigureAwait(false);
            }
        }

        #endregion

        #region Helpers

        private async Task<bool> IsMrContainceAsync(string mrUrl, string chatId)
        {
            if (string.IsNullOrEmpty(mrUrl) || string.IsNullOrEmpty(chatId)) return false;

            var conversations = _dbContext.Conversations.GetAll();

            if (conversations == null || !conversations.Any()) return false;

            var mrChat = conversations.FirstOrDefault(c => c.MRChat.Id.Equals(chatId));

            if (mrChat?.ListOfMerge == null || !mrChat.ListOfMerge.Any()) return false;

            return mrChat.ListOfMerge.Any(c => c.MrUrl.Equals(mrUrl, StringComparison.InvariantCultureIgnoreCase));
        }

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

        private static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
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

        private IKeyboard GetKeyboardForDefaultMessage()
        {
            return new ReplyKeyboardMarkup()
            {
                IsHideKeyboardAfterClick = true,
                Keyboard = new List<List<KeyboardButton>>()
                {
                    new List<KeyboardButton>()
                    {
                        new KeyboardButton()
                        {
                            Text = HELP,
                        },
                        new KeyboardButton()
                        {
                            Text = COMMON_DOCUMENT,
                        },
                        new KeyboardButton()
                        {
                            Text = $"{GET_STATISTIC}_getalldata",
                        },
                    },
                    new List<KeyboardButton>()
                    {
                        new KeyboardButton()
                        {
                            Text = $"{GET_STATISTIC}_getmrreaction",
                        },
                        new KeyboardButton()
                        {
                            Text = $"{GET_STATISTIC}_getusermrreaction",
                        },
                        new KeyboardButton()
                        {
                            Text = $"{GET_STATISTIC}_getunmarked",
                        },
                    },
                },
            };
        }

        private void AddButtonForRequest(SendMessageRequest message, string mrLink, List<string> ticketLinks, int okCount = 0, int badCount = 0)
        {
            var lineButton = new List<InlineKeyboardButton>()
            {
                new InlineKeyboardButton()
                {
                    Text = "👍" + (okCount == 0 ? string.Empty : $" ({okCount})"),
                    CallbackData = $"/success reaction",
                },
                new InlineKeyboardButton()
                {
                    Text = "MR link",
                    Url = mrLink,
                },
                new InlineKeyboardButton()
                {
                    Text = "🚫" + (badCount == 0 ? string.Empty : $" ({badCount})"),
                    CallbackData = @"/bad reaction",
                },
            };

            var ticketButtons = new List<InlineKeyboardButton>
            {
                new InlineKeyboardButton()
                {
                    Text = "Stat 📈",
                    CallbackData = "/get stat",
                },
            };

            foreach (var ticketLink in ticketLinks.Where(c => !string.IsNullOrEmpty(c)))
            {
                var text = Regex.Match(ticketLink, TICKET_NUMBER_PATTERN).Value;
                ticketButtons.Add(new InlineKeyboardButton()
                {
                    Text = text,
                    Url = ticketLink,
                });
            }

            var buttons = new List<List<InlineKeyboardButton>>()
            {
                new List<InlineKeyboardButton>(lineButton),
                new List<InlineKeyboardButton>(ticketButtons),
            };

            message.ReplyMarkup = new InlineKeyboardMarkup()
            {
                InlineKeyboardButtons = buttons,
            };
        }

        private DateTime ConvertToDate(string date)
        {
            DateTimeFormatInfo dtfi = CultureInfo.CreateSpecificCulture("en-US").DateTimeFormat;
            return Convert.ToDateTime(date, new DateTimeFormatInfo()
            {
                ShortDatePattern = dtfi.ShortDatePattern,
            });
        }

        #endregion
    }
}
