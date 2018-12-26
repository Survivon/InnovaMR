using InnovaMRBot.Models;
using InnovaMRBot.Repository;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
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

        private const string GET_STATISTIC = "/get stat";

        private const string GET_UNMARKED_MR = "/get MR";

        private const string GET_MY_UNMARKED_MR = "/get my MR";

        private const string START_READ_ALL_MESSAGE = "/get all message";

        private const string MR_PATTERN = @"https?:\/\/gitlab.fortia.fr\/Fortia\/Innova\/merge_requests\/[0-9]+";

        private const string TICKET_PATTERN = @"https?:\/\/fortia.atlassian.net\/browse\/\w+-[0-9]+";

        private const string CONVERSATION_KEY = "conversation";

        private const string USER_SETTING_KEY = "users";

        private const string GUID_PATTERN = @"(\{){0,1}[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}(\}){0,1}";

        private const string TICKET_NUMBER_PATTERN = @"\w+-[0-9]+";

        private const int MAX_COUNT_OF_ADMINS = 2;

        private const string START = @"/start";

        private const string HELP = @"/help";

        private const string COMMON_DOCUMENT = @"/getcommondocument";

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
                    // get start message
                    answerMessages.Add(new SendMessageRequest()
                    {
                        ChatId = update.Message.Chat.Id.ToString(),
                        Text = "Hi! I'm Bot for help to work with MR for Innova 😊 If you have some question please send me /help or visit site http://innovamrbot.azurewebsites.net/",
                        ReplyMarkup = new ReplyKeyboardMarkup()
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
                                },
                            },
                        },
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
For all of this statistics you can add start and end date of publish date(For ex. <b>/get stat getalldata 24/11/2018 28/11/2018</b>)",
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
                SetupMessageReactionAsync(update).ConfigureAwait(false);
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
            var needUser = users.FirstOrDefault(u => u.UserId.Equals(userId));
            if (needUser == null)
            {
                var savedUser = new User()
                {
                    Name = GetUserFullName(update.CallbackQuery.Sender),
                    UserId = userId,
                };

                AddOrUpdateUser(savedUser);

                needUser = savedUser;
            }

            if (needMr != null)
            {
                if (needMr.Reactions.Any(r => r.UserId.Equals(userId)))
                {
                    await _telegramService.SendCallbackAnswerAsync(new AnswerCallbackQueryRequest()
                    {
                        IsNeedShowAlert = true,
                        Text = "Your reaction is already saved",
                        CallbackId = update.CallbackQuery.Id,
                    });

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

                var reaction = new Models.MessageReaction()
                {
                    ReactionTime = DateTimeOffset.UtcNow,
                    UserId = needUser.UserId,
                };

                reaction.SetReactionInMinutes(needMr.PublishDate.Value);

                needMr.Reactions.Add(reaction);

                AddButtonForRequest(returnedMessage, needMr.MrUrl, needMr.TicketsUrl.Split(';').ToList(), needMr.Reactions.Count);

                returnedMessage.ChatId = conversation.MRChat.Id;
                returnedMessage.Text = needMr.AllText;
                returnedMessage.EditMessageId = messageId;

                await _telegramService.EditMessageAsync(returnedMessage);
                _dbContext.Conversations.Update(conversation);
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

                    if (versionOffMr.Reactions.Any(r => r.UserId.Equals(userId)))
                    {
                        await _telegramService.SendCallbackAnswerAsync(new AnswerCallbackQueryRequest()
                        {
                            IsNeedShowAlert = true,
                            Text = "Your reaction is already saved",
                            CallbackId = update.CallbackQuery.Id,
                        });

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

                    var reaction = new Models.MessageReaction()
                    {
                        ReactionTime = DateTimeOffset.UtcNow,
                        UserId = needUser.UserId,
                    };

                    reaction.SetReactionInMinutes(versionOffMr.PublishDate.Value);

                    versionOffMr.Reactions.Add(reaction);

                    AddButtonForRequest(returnedMessage, versionedMr.MrUrl, versionedMr.TicketsUrl.Split(';').ToList(), versionOffMr.Reactions.Count);

                    returnedMessage.ChatId = conversation.MRChat.Id;
                    returnedMessage.Text = versionOffMr.AllDescription;
                    returnedMessage.EditMessageId = messageId;

                    await _telegramService.EditMessageAsync(returnedMessage);
                    _dbContext.Conversations.Update(conversation);
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
            var userId = message.ChanelMessage.ForwardSender.Id.ToString();
            var responseMessage = new SendMessageRequest()
            {
                ChatId = convesationId,
            };

            var users = _dbContext.Users.GetAll();
            var needUser = users.FirstOrDefault(u => u.UserId.Equals(userId));
            if (needUser == null)
            {
                var savedUser = new User()
                {
                    Name = GetUserFullName(message.Message.ForwardSender),
                    UserId = message.ChanelMessage.ForwardSender.Id.ToString(),
                };

                AddOrUpdateUser(savedUser);
            }

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
            var userId = message.Message.Sender.Id.ToString();

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

            var users = _dbContext.Users.GetAll();
            var needUser = users.FirstOrDefault(u => u.UserId.Equals(userId));
            if (needUser == null)
            {
                var savedUser = new User()
                {
                    Name = GetUserFullName(message.Message.Sender),
                    UserId = userId,
                };

                AddOrUpdateUser(savedUser);

                needUser = savedUser;
            }

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

                responseMessage.ChatId = conversation.MRChat.Id;
                responseMessage.Text = $"{messageText} \nby {needUser.Name}"; ;
                AddButtonForRequest(responseMessage, mrUrl, needMr.TicketsUrl.Split(';').ToList());

                var resMessage = await _telegramService.SendMessageAsync(responseMessage);
                responseMessageForUser.Text = "Well done! I'll send it 😊";

                _telegramService.SendMessageAsync(responseMessageForUser).ConfigureAwait(false);

                var versionedTicket = new VersionedMergeRequest()
                {
                    OwnerMergeId = needMr.TelegramMessageId,
                    PublishDate = DateTimeOffset.UtcNow,
                    Id = resMessage.Id.ToString(),
                    AllDescription = messageText,
                };

                var description = new Regex(TICKET_PATTERN)
                    .Replace(new Regex(MR_PATTERN).Replace(messageText, string.Empty), string.Empty).Trim();

                var lineOfMessage = description.Split('\r').ToList();
                var firstLine = lineOfMessage.FirstOrDefault();

                if (_changesNotation.Any(c => c.Equals(firstLine, StringComparison.InvariantCultureIgnoreCase)))
                {
                    lineOfMessage.RemoveAt(0);
                    description = string.Join('\r', lineOfMessage);
                }

                versionedTicket.Description = description;

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
                mrMessage.TelegramMessageId = message.Message.Id.ToString();

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
                responseMessage.Text = $"{messageText} \nby {needUser.Name}";

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
            if (components.Length > 1)
            {
                startDate = new DateTimeOffset(Convert.ToDateTime(components[1]));
            }

            var endDate = default(DateTimeOffset);
            if (components.Length > 2)
            {
                endDate = new DateTimeOffset(Convert.ToDateTime(components[2]));
            }

            var result = string.Empty;
            var conversations = _dbContext.Conversations.GetAll();
            var conversation = conversations.FirstOrDefault();
            var users = _dbContext.Users.GetAll().ToList();

            if (conversation != null)
            {
                switch (command)
                {
                    case "getalldata":
                        result = StatHtmlBuilder.GetAllData(conversation.ListOfMerge.ToList(), users, startDate, endDate);
                        break;
                    case "getmrreaction":
                        result = StatHtmlBuilder.GetMRReaction(conversation.ListOfMerge.ToList(), users, startDate, endDate);
                        break;
                    case "getusermrreaction":
                        result = StatHtmlBuilder.GetUsersMRReaction(conversation.ListOfMerge.ToList(), users, startDate, endDate);
                        break;
                    case "getunmarked":
                        result = StatHtmlBuilder.GetUnmarkedCountMergePerDay(conversation.ListOfMerge.ToList(), users, startDate, endDate);
                        break;
                }

                _telegramService.SendDocumentAsync(new SendDocumentRequest()
                {
                    ChatId = update.Message.Chat.Id.ToString(),
                    Document = string.Empty
                }, result).ConfigureAwait(false);
            }
        }

        #endregion

        #region Skype part

        public async Task<Activity> ReturnMessage(ITurnContext turnContext)
        {
            var returnMessage = new Activity();

            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                var message = turnContext.Activity.Text;

                //if (message.Equals(MARK_MR_CONVERSATION))
                //{
                //    return await SetupMRConversationAsync(turnContext);
                //}
                //else if (new Regex(GUID_PATTERN).IsMatch(message) && message.StartsWith($"{MARK_ALERT_CONVERSATION}"))
                //{
                //    if (Guid.TryParse(new Regex(GUID_PATTERN).Match(message).Value, out var syncId))
                //        return await SetupAlertConversationAsync(turnContext, syncId);
                //    else return turnContext.Activity.CreateReply();
                //}
                //else if (message.Equals(REMOVE_MR_CONVERSATION))
                //{
                //    return await RemoveMrConversationAsync(turnContext);
                //}
                //else
                if (message.Equals(REMOVE_ALERT_CONVERSATION))
                {
                    return await RemoveAlertConversationAsync(turnContext);
                }
                //else if (message.Equals(SETUP_ADMIN))
                //{
                //    return await AddAdminAsync(turnContext);
                //}
                //else if (message.Equals(REMOVE_ADMIN))
                //{
                //    return await RemoveAdminAsync(turnContext);
                //}
                //else if (message.StartsWith(GET_STATISTIC))
                //{
                //    return await GetStatisticsAsync(turnContext);
                //}
                //else if (message.Equals(GET_UNMARKED_MR))
                //{
                //    return await GetUnMarkedMergeRequestAsync(turnContext);
                //}
                //else if (message.Equals(GET_MY_UNMARKED_MR))
                //{
                //    return await GetMyUnMarkedMergeRequestAsync(turnContext);
                //}
                //else
                //{
                //    return await GetMrMessageAsync(turnContext);
                //}
            }
            else if (turnContext.Activity.Type == ActivityTypes.MessageReaction)
            {
                //
            }

            return returnMessage;
        }

        ////TODO: update method
        //private async Task SetReactionForMessage(ITurnContext context)
        //{
        //    var reactions = context.Activity.ReactionsAdded.ToList();
        //    var users = await GetUsersAsync();
        //    var needUser = users.Users.FirstOrDefault(u => u.UserId.Equals(context.Activity.From.Id));

        //    if (needUser == null)
        //    {
        //        var savedUser = new User()
        //        {
        //            Name = context.Activity.From.Name,
        //            UserId = context.Activity.From.Id,
        //        };

        //        await AddOrUpdateUserAsync(savedUser);

        //        needUser = savedUser;
        //    }

        //    var messageId = context.Activity.ReplyToId;

        //    foreach (var messageReaction in reactions)
        //    {
        //        var type = messageReaction.Type;
        //    }
        //}

        #region Conversation setup

        private async Task<Activity> RemoveAlertConversationAsync(ITurnContext context)
        {
            var convesationId = context.Activity.Conversation.Id;
            var userId = context.Activity.From.Id;
            var responseMessage = context.Activity.CreateReply();
            responseMessage.TextFormat = "plain";

            var users = _dbContext.Users.GetAll();
            var needUser = users.FirstOrDefault(u => u.UserId.Equals(userId));
            if (needUser == null)
            {
                var savedUser = new User()
                {
                    Name = context.Activity.From.Name,
                    UserId = context.Activity.From.Id,
                };

                AddOrUpdateUser(savedUser);
            }

            var conversations = _dbContext.Conversations.GetAll();
            //var needConversation = conversations.FirstOrDefault(c => c.AlertChat.Id.Equals(convesationId));
            //if (needConversation == null)
            //{
            //    responseMessage.Text =
            //        "This is not a Alert conversation or you don't add any conversation. Try in Alert conversation ;)";
            //}
            //else
            //{
            //    //if (needConversation.Admins.Any(u => u.UserId.Equals(userId)))
            //    //{
            //    //    responseMessage.Text = $"Congratulation, you remove alert conversation. Please setup alert conversation by sync id: {needConversation.MRChat.SyncId}";

            //    //    needConversation.AlertChat = null;
            //    //    await UpdateConversationAsync(needConversation);
            //    //}
            //    //else
            //    //{
            //    //    responseMessage.Text = "You don't have permission for remove this conversation!";
            //    //}
            //}

            return responseMessage;
        }

        //private async Task<Activity> RemoveMrConversationAsync(ITurnContext context)
        //{
        //    var convesationId = context.Activity.Conversation.Id;
        //    var userId = context.Activity.From.Id;
        //    var responseMessage = context.Activity.CreateReply();
        //    responseMessage.TextFormat = "plain";

        //    var users = await GetUsersAsync();
        //    var needUser = users.Users.FirstOrDefault(u => u.UserId.Equals(userId));
        //    if (needUser == null)
        //    {
        //        var savedUser = new User()
        //        {
        //            Name = context.Activity.From.Name,
        //            UserId = context.Activity.From.Id,
        //        };

        //        await AddOrUpdateUserAsync(savedUser);
        //    }

        //    var conversations = await GetCurrentConversationsAsync();

        //    var needConversation = conversations.BotConversation.FirstOrDefault(c => c.MRChat.Id.Equals(convesationId));
        //    if (needConversation == null)
        //    {
        //        responseMessage.Text =
        //            "This is not a MR's conversation or you don't add any conversation. Try in MR's conversation ;)";
        //    }
        //    else
        //    {
        //        if (needConversation.Admins.Any(u => u.UserId.Equals(userId)))
        //        {
        //            responseMessage.Text = "Congratulation, you remove all data with linked for current conversation";
        //            // TODO: add get stat and send before remove
        //            await RemoveConversationAsync(needConversation);
        //        }
        //        else
        //        {
        //            responseMessage.Text = "You don't have permission for remove this conversation!";
        //        }
        //    }

        //    return responseMessage;
        //}

        //private async Task<Activity> SetupMRConversationAsync(ITurnContext context)
        //{
        //    var conversationId = context.Activity.Conversation.Id;

        //    var resultMessage = context.Activity.CreateReply();

        //    var conversations = await GetCurrentConversationsAsync();

        //    if (!conversations.BotConversation.Any())
        //    {
        //        var syncId = Guid.NewGuid();
        //        var chatSetting = new ChatSetting()
        //        {
        //            Id = conversationId,
        //            IsMRChat = true,
        //            SyncId = syncId,
        //            Name = context.Activity.Conversation.Name,
        //            BaseActivity = context.Activity,
        //        };

        //        var newConversation = new ConversationSetting()
        //        {
        //            MRChat = chatSetting,
        //        };

        //        await SaveConversationAsync(newConversation);

        //        resultMessage.Text = $"Current chat is setup as MR with sync id: {syncId}";
        //    }

        //    return resultMessage;
        //}

        private async Task<Activity> SetupAlertConversationAsync(ITurnContext context, Guid syncId)
        {
            var conversationId = context.Activity.Conversation.Id;
            var responseMessage = context.Activity.CreateReply();
            responseMessage.TextFormat = "plain";

            var conversations = _dbContext.Conversations.GetAll();

            if (!conversations.Any())
            {
                responseMessage.Text = "Have not setup any conversation to Bot 😊";
            }
            else
            {
                var neededConversation =
                    conversations.FirstOrDefault(c => c.MRChat.SyncId.Equals(syncId));
                if (neededConversation == null)
                {
                    responseMessage.Text = $"Didn't setup MR chat for current sync id: {syncId}";
                }
                else
                {
                    //neededConversation.AlertChat = new ChatSetting()
                    //{
                    //    Id = conversationId,
                    //    IsAlertChat = true,
                    //    SyncId = syncId,
                    //    Name = context.Activity.Conversation.Name,
                    //};

                    //_dbContext.Conversations.Update(neededConversation);
                    responseMessage.Text = $"This chat is setup as alert chat for conversation {neededConversation.MRChat.Name}";
                }
            }

            return responseMessage;
        }

        //private async Task<Activity> AddAdminAsync(ITurnContext context)
        //{
        //    var responseMessage = new Activity();

        //    responseMessage = await GetMessageWithCheckChatAsync(
        //        async (conversation) =>
        //    {
        //        if (conversation.Admins.Count >= MAX_COUNT_OF_ADMINS)
        //        {
        //            return "Admin list for current conversation is full";
        //        }
        //        else
        //        {
        //            var users = await GetUsersAsync();
        //            var needUser = users.Users.FirstOrDefault(u => u.UserId.Equals(context.Activity.From.Id));
        //            if (needUser == null)
        //            {
        //                var savedUser = new User()
        //                {
        //                    Name = context.Activity.From.Name,
        //                    UserId = context.Activity.From.Id,
        //                };

        //                await AddOrUpdateUserAsync(savedUser);

        //                needUser = savedUser;
        //            }

        //            conversation.Admins.Add(needUser);

        //            await SaveConversationAsync(conversation);

        //            return $"{needUser.Name} add like admin";
        //        }
        //    }, context);

        //    return responseMessage;
        //}

        //private async Task<Activity> RemoveAdminAsync(ITurnContext context)
        //{
        //    var responseMessage = new Activity();

        //    responseMessage = await GetMessageWithCheckChatAsync(
        //        async (conversation) =>
        //        {
        //            var users = await GetUsersAsync();
        //            var needUser = users.Users.FirstOrDefault(u => u.UserId.Equals(context.Activity.From.Id));
        //            if (needUser == null)
        //            {
        //                var savedUser = new User()
        //                {
        //                    Name = context.Activity.From.Name,
        //                    UserId = context.Activity.From.Id,
        //                };

        //                await AddOrUpdateUserAsync(savedUser);

        //                needUser = savedUser;
        //            }

        //            if (conversation.Admins.Any(u => u.UserId.Equals(needUser.UserId)))
        //            {
        //                conversation.Admins.RemoveAll(u => u.UserId.Equals(needUser.UserId));
        //                return "You have been removed from admin of current conversation";
        //            }

        //            return $"Good joke xD You are admin of chat. Try next time ;)";
        //        }, context);

        //    return responseMessage;
        //}

        #endregion

        #region Merge Requst

        //        private async Task<Activity> GetMrMessageAsync(ITurnContext context)
        //        {
        //            var userId = context.Activity.From.Id;
        //            var responseMessage = context.Activity.CreateReply(string.Empty);
        //            responseMessage.TextFormat = "plain";

        //            var messageText = context.Activity.Text;

        //            var mrRegex = new Regex(MR_PATTERN);
        //            var mrUrlMatch = mrRegex.Match(messageText);
        //            var mrUrl = mrUrlMatch.Value;

        //            var conversation = await GetConversationByIdAsync(context.Activity.Conversation.Id);

        //            if (await IsMrContainceAsync(mrUrl, context.Activity.Conversation.Id))
        //            {
        //                // for updatedTicket
        //                var needMr = conversation.ListOfMerge.FirstOrDefault(m => m.MrUrl.Equals(mrUrl));
        //                if (needMr == null)
        //                {
        //                    return responseMessage;
        //                }

        //                needMr.IsHadAlreadyChange = true;
        //                needMr.CountOfChange++;

        //                needMr.VersionedSetting.Add(new VersionedMergeRequest()
        //                {
        //                    OwnerMergeId = needMr.Id,
        //                    PublishDate = context.Activity.Timestamp ?? DateTimeOffset.UtcNow,
        //                    Id = context.Activity.Id,
        //                });

        //                await SaveConversationAsync(conversation);
        //            }
        //            else
        //            {
        //                var users = await GetUsersAsync();
        //                var needUser = users.Users?.FirstOrDefault(u => u.UserId.Equals(userId));
        //                if (needUser == null)
        //                {
        //                    var savedUser = new User()
        //                    {
        //                        Name = context.Activity.From.Name,
        //                        UserId = userId,
        //                    };

        //                    await AddOrUpdateUserAsync(savedUser);

        //                    needUser = savedUser;
        //                }

        //                var mrMessage = new MergeSetting { MrUrl = mrUrl };

        //                var ticketRegex = new Regex(TICKET_PATTERN);

        //                var ticketMatches = ticketRegex.Matches(messageText);

        //                var description = messageText.Replace(mrUrl, string.Empty);

        //                foreach (Match ticketMatch in ticketMatches)
        //                {
        //                    mrMessage.TicketsUrl.Add(ticketMatch.Value);
        //                    description = description.Replace(ticketMatch.Value, string.Empty);
        //                }

        //                mrMessage.PublishDate = context.Activity.Timestamp ?? DateTimeOffset.UtcNow;
        //                mrMessage.Id = context.Activity.Id;

        //                var lineOfMessage = description.Split('\r').ToList();
        //                var firstLine = lineOfMessage.FirstOrDefault();

        //                if (_changesNotation.Any(c => c.Equals(firstLine, StringComparison.InvariantCultureIgnoreCase)))
        //                {
        //                    lineOfMessage.RemoveAt(0);
        //                    description = string.Join('\r', lineOfMessage);
        //                }

        //                mrMessage.Description = description.Trim(new char[] { '\r', '\n' });
        //                mrMessage.Owner = needUser;

        //                conversation.ListOfMerge.Add(mrMessage);

        //                await SaveConversationAsync(conversation);
        //            }

        //            return responseMessage;
        //        }

        //        private async Task<Activity> GetUnMarkedMergeRequestAsync(ITurnContext context)
        //        {
        //            var responseMessages = await GetMessageWithCheckChatAsync(
        //                async (conversation) =>
        //                {
        //                    var mrs = conversation.ListOfMerge.Where(m => m.Reactions.Count < 2).ToList();

        //                    if (!mrs.Any())
        //                    {
        //                        return "Good job guys, you don't have any merge request which must be reviewed 😊";
        //                    }

        //                    var builder = new StringBuilder();

        //                    builder.Append($@"---------------------------

        //You have {mrs.Count} unchecked merge requests
        //");

        //                    foreach (var mergeSetting in mrs)
        //                    {
        //                        builder.Append($@"\t- {mergeSetting.Owner.Name} publish MR with link {mergeSetting.MrUrl}
        //");
        //                    }

        //                    builder.Append(@"
        //Thanks 😊 
        //---------------------------");

        //                    return builder.ToString();
        //                }, context);


        //            return responseMessages;
        //        }

        //        private async Task<Activity> GetMyUnMarkedMergeRequestAsync(ITurnContext context)
        //        {
        //            var userId = context.Activity.From.Id;

        //            var responseMessages = await GetMessageWithCheckChatAsync(
        //                async (conversation) =>
        //                {
        //                    var mrs = conversation.ListOfMerge.Where(m => m.Reactions.Count < 2 && m.Owner.UserId.Equals(userId)).ToList();

        //                    if (!mrs.Any())
        //                    {
        //                        return "Good job, you don't have any merge request which must be reviewed 😊";
        //                    }

        //                    var builder = new StringBuilder();

        //                    builder.Append($@"---------------------------

        //You have {mrs.Count} unchecked merge requests
        //");

        //                    foreach (var mergeSetting in mrs)
        //                    {
        //                        builder.Append($@"\t- {mergeSetting.Owner.Name} publish MR with link {mergeSetting.MrUrl}
        //");
        //                    }

        //                    builder.Append(@"
        //Thanks 😊 
        //---------------------------");

        //                    return builder.ToString();
        //                }, context);


        //            return responseMessages;
        //        }

        #endregion

        #region Stats

        //private async Task<Activity> GetStatisticsAsync(ITurnContext context)
        //{
        //    var responseMessage = await GetMessageWithCheckChatAsync(
        //        async (conversation) =>
        //        {
        //            var message = context.Activity.Text;
        //            message = message.Replace(GET_STATISTIC, string.Empty);
        //            var components = message.Split(' ');
        //            var command = components[0];
        //            var startDate = default(DateTimeOffset);
        //            if (components.Length > 1)
        //            {
        //                startDate = new DateTimeOffset(Convert.ToDateTime(components[1]));
        //            }

        //            var endDate = default(DateTimeOffset);
        //            if (components.Length > 2)
        //            {
        //                endDate = new DateTimeOffset(Convert.ToDateTime(components[2]));
        //            }

        //            var result = new ResponseMessage();

        //            switch (command)
        //            {
        //                case "getalldata":
        //                    result = StatHtmlBuilder.GetAllData(conversation.ListOfMerge, startDate, endDate);
        //                    break;
        //                case "getmrreaction":
        //                    result = StatHtmlBuilder.GetMRReaction(conversation.ListOfMerge, startDate, endDate);
        //                    break;
        //                case "getusermrreaction":
        //                    result = StatHtmlBuilder.GetUsersMRReaction(conversation.ListOfMerge, startDate, endDate);
        //                    break;
        //                case "getunmarked":
        //                    result = StatHtmlBuilder.GetUnmarkedCountMergePerDay(conversation.ListOfMerge, startDate,
        //                        endDate);
        //                    break;
        //            }

        //            return result.Message;
        //        }, context);

        //    responseMessage.TextFormat = "plain";

        //    responseMessage.Attachments = new List<Attachment>()
        //    {
        //        new Attachment()
        //        {
        //            Name = "Stat",
        //            ContentUrl = responseMessage.Text,
        //            ContentType = DownloadController.GetContentType(responseMessage.Text),
        //        },
        //    };

        //    return responseMessage;
        //}

        #endregion

        #endregion

        #region Helpers

        //private async Task<Activity> GetMessageWithCheckChatAsync(Func<ConversationSetting, Task<string>> successAction, ITurnContext context)
        //{
        //    var conversationId = context.Activity.Conversation.Id;

        //    var activity = new Activity();

        //    var conversations = await GetCurrentConversationsAsync();

        //    if (conversations.BotConversation.Any())
        //    {
        //        var conversation =
        //            conversations.BotConversation.FirstOrDefault(c => c.AlertChat.Id.Equals(conversationId));
        //        if (conversation == null)
        //        {
        //            var mrConversation =
        //                conversations.BotConversation.FirstOrDefault(c => c.MRChat.Id.Equals(conversationId));
        //            if (mrConversation != null)
        //            {
        //                activity = mrConversation.MRChat.BaseActivity.CreateReply();
        //                activity.Text =
        //                    $"This is not conversation for input command, please select conversation named {mrConversation.AlertChat?.Name ?? string.Empty}";
        //                activity.TextFormat = "plain";
        //            }
        //            else
        //            {
        //                activity = context.Activity.CreateReply();
        //                activity.Text = "I don't know why are you want to do something in useless conversation :(";
        //                activity.TextFormat = "plain";
        //            }
        //        }
        //        else
        //        {
        //            activity = conversation.AlertChat.BaseActivity.CreateReply();
        //            activity.Text = await successAction.Invoke(conversation);
        //            activity.TextFormat = "plain";
        //        }
        //    }

        //    return activity;
        //}

        private async Task<bool> IsMrContainceAsync(string mrUrl, string chatId)
        {
            if (string.IsNullOrEmpty(mrUrl) || string.IsNullOrEmpty(chatId)) return false;

            var conversations = _dbContext.Conversations.GetAll();

            if (conversations == null || !conversations.Any()) return false;

            var mrChat = conversations.FirstOrDefault(c => c.MRChat.Id.Equals(chatId));

            if (mrChat?.ListOfMerge == null || !mrChat.ListOfMerge.Any()) return false;

            return mrChat.ListOfMerge.Any(c => c.MrUrl.Equals(mrUrl, StringComparison.InvariantCultureIgnoreCase));
        }

        //private async Task SaveConversationAsync(ConversationSetting conversation)
        //{
        //    if (_dbContext == null) return;

        //    await _dbContext.ConversationSettings.AddAsync(conversation);
        //    await _dbContext.SaveChangesAsync();

        //    //return;

        //    //var conversations = await GetCurrentConversationsAsync();

        //    //conversations.BotConversation.RemoveAll(c => c.Id.Equals(conversation.Id));

        //    //conversations.BotConversation.Add(conversation);

        //    //await ConversationState.Storage.WriteAsync(new Dictionary<string, object>()
        //    //{
        //    //    {CONVERSATION_KEY, conversations},
        //    //});
        //}

        //private async Task UpdateConversationAsync(ConversationSetting conversationSetting)
        //{
        //    if (_dbContext == null) return;

        //    _dbContext.ConversationSettings.Update(conversationSetting);
        //    await _dbContext.SaveChangesAsync();
        //}

        //private async Task RemoveConversationAsync(ConversationSetting conversation)
        //{
        //    _dbContext.ConversationSettings.Remove(conversation);
        //    await _dbContext.SaveChangesAsync();

        //    //var conversations = await GetCurrentConversationsAsync();

        //    //conversations.BotConversation.RemoveAll(c => c.Id.Equals(conversation.Id));

        //    //await ConversationState.Storage.WriteAsync(new Dictionary<string, object>()
        //    //{
        //    //    {CONVERSATION_KEY, conversations},
        //    //});
        //}

        ////private async Task<ConversationSetting> GetCurrentConversationsAsync()
        ////{
        ////    return await _dbContext.ConversationSettings.FirstOrDefaultAsync();

        ////    //var storeProvider = ConversationState.Storage;

        ////    //var result = await storeProvider.ReadAsync(new[] { CONVERSATION_KEY }, CancellationToken.None);

        ////    //if (result == null || !result.Any() || !result.ContainsKey(CONVERSATION_KEY))
        ////    //{
        ////    //    await GetNewConversationAsync(storeProvider);
        ////    //}

        ////    //if (result.TryGetValue(CONVERSATION_KEY, out var conversations) && conversations is Conversations conversations1)
        ////    //{
        ////    //    return conversations1;
        ////    //}

        ////    //return await GetNewConversationAsync(storeProvider);
        ////}

        ////private async Task<Conversations> GetNewConversationAsync(IStorage storeProvider)
        ////{
        ////    var newConversations = new Conversations();

        ////    await storeProvider.WriteAsync(
        ////        new Dictionary<string, object>()
        ////    {
        ////        { CONVERSATION_KEY, newConversations },
        ////    }, CancellationToken.None);

        ////    return newConversations;
        ////}

        ////private async Task<UserSetting> GetUsersAsync()
        ////{
        ////    var storeProvider = ConversationState.Storage;

        ////    var result = await storeProvider.ReadAsync(new[] { USER_SETTING_KEY }, CancellationToken.None);

        ////    if (result == null || !result.Any() || !result.ContainsKey(USER_SETTING_KEY))
        ////    {
        ////        await GetNewUserAsync(storeProvider);
        ////    }

        ////    if (result.TryGetValue(CONVERSATION_KEY, out var userObject) && userObject is UserSetting user)
        ////    {
        ////        return user;
        ////    }

        ////    return await GetNewUserAsync(storeProvider);
        ////}

        private void AddOrUpdateUser(User user)
        {
            var users = _dbContext.Users.GetAll();

            if (users.Any(u => u.UserId.Equals(user.UserId)))
            {
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

        private void AddButtonForRequest(SendMessageRequest message, string mrLink, List<string> ticketLinks, int okCount = 0)
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
            };

            var ticketButtons = new List<InlineKeyboardButton>();

            foreach (var ticketLink in ticketLinks)
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

        #endregion

    }
}
