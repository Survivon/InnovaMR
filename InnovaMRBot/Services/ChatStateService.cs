using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using InnovaMRBot.Controllers;
using InnovaMRBot.Helpers;
using InnovaMRBot.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using TelegramBotApi.Models;
using TelegramBotApi.Models.Keyboard;
using TelegramBotApi.Telegram;
using TelegramBotApi.Telegram.Events;
using TelegramBotApi.Telegram.Request;
using Conversations = InnovaMRBot.Models.Conversations;
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

        private const int MAX_COUNT_OF_ADMINS = 2;

        private readonly List<string> _changesNotation = new List<string>()
        {
            "UPDATED",
            "ИЗМЕНЕНО"
        };

        #endregion

        private readonly Telegram _telegramService;

        public CustomConversationState ConversationState { get; }
        
        public ChatStateService(CustomConversationState conversationState, Telegram telegram)
        {
            ConversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
            _telegramService = telegram;
            _telegramService.OnUpdateResieve += TelegramServiceOnOnUpdateResieve;
        }

        private void TelegramServiceOnOnUpdateResieve(object sender, UpdateEventArgs e)
        {
            
        }

        #region Telegram part

        private async Task<SendMessageRequest> SetupMRConversationAsync(Update message)
        {
            var conversationId = message.Message.Chat.Id.ToString();

            var resultMessage = new SendMessageRequest()
            {
                ChatId = conversationId,
            };

            var conversations = await GetCurrentConversationsAsync();

            if (!conversations.BotConversation.Any())
            {
                var syncId = Guid.NewGuid();
                var chatSetting = new ChatSetting()
                {
                    Id = conversationId,
                    IsMRChat = true,
                    SyncId = syncId,
                    Name = message.Message.Chat.FirstName + message.Message.Chat.LastName,
                };

                var newConversation = new ConversationSetting()
                {
                    MRChat = chatSetting,
                };

                await SaveConversationAsync(newConversation);

                resultMessage.Text = $"Current chat is setup as MR with sync id: {syncId}";
            }
            
            return resultMessage;
        }

        private async Task<SendMessageRequest> RemoveMrConversationAsync(Update message)
        {
            var convesationId = message.Message.Chat.Id.ToString();
            var userId = message.Message.ForwardSender.Id.ToString();
            var responseMessage = new SendMessageRequest()
            {
                ChatId = convesationId,
            };

            var users = await GetUsersAsync();
            var needUser = users.Users.FirstOrDefault(u => u.UserId.Equals(userId));
            if (needUser == null)
            {
                var savedUser = new User()
                {
                    Name = GetUserFullName(message.Message.ForwardSender),
                    UserId = message.Message.ForwardSender.Id.ToString(),
                };

                await AddOrUpdateUserAsync(savedUser);
            }

            var conversations = await GetCurrentConversationsAsync();

            var needConversation = conversations.BotConversation.FirstOrDefault(c => c.MRChat.Id.Equals(convesationId));
            if (needConversation == null)
            {
                responseMessage.Text =
                    "This is not a MR's conversation or you don't add any conversation. Try in MR's conversation ;)";
            }
            else
            {
                if (needConversation.Admins.Any(u => u.UserId.Equals(userId)))
                {
                    responseMessage.Text = "Congratulation, you remove all data with linked for current conversation";
                    // TODO: add get stat and send before remove
                    await RemoveConversationAsync(needConversation);
                }
                else
                {
                    responseMessage.Text = "You don't have permission for remove this conversation!";
                }
            }

            return responseMessage;
        }

        private async Task<List<SendMessageRequest>> GetMRMessageAsync(Update message)
        {
            var convesationId = message.Message.Chat.Id.ToString();
            var userId = message.Message.ForwardSender.Id.ToString();

            var responseMessageChain = new List<SendMessageRequest>();

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

            var conversations = await GetCurrentConversationsAsync();

            var conversation =
                conversations.BotConversation.FirstOrDefault(c => c.Partisipants.Any(p => p.UserId == userId));

            if (await IsMrContainceAsync(mrUrl, conversation.MRChat.Id))
            {
                // for updatedTicket
                var needMr = conversation.ListOfMerge.FirstOrDefault(m => m.MrUrl.Equals(mrUrl));
                if (needMr == null)
                {
                    responseMessageForUser.Text = "I can not to find any MR for update";
                    return new List<SendMessageRequest>(){ responseMessageForUser };
                }

                needMr.IsHadAlreadyChange = true;
                needMr.CountOfChange++;

                needMr.VersionedSetting.Add(new VersionedMergeRequest()
                {
                    OwnerMergeId = needMr.Id,
                    PublishDate = new DateTimeOffset(UnixTimeStampToDateTime((double)message.Message.ForwardDate)),
                    Id = message.Message.Id.ToString(),
                });

                await SaveConversationAsync(conversation);

                responseMessage.ChatId = conversation.MRChat.Id;
                responseMessage.Text = messageText;
                AddButtonForRequest(responseMessage, message.Message.Id.ToString(), mrUrl);

                responseMessageChain.Add(responseMessage);

                responseMessageForUser.Text = "Well done! I'll send it :)";

                responseMessageChain.Add(responseMessageForUser);
            }
            else
            {
                var users = await GetUsersAsync();
                var needUser = users.Users?.FirstOrDefault(u => u.UserId.Equals(userId));
                if (needUser == null)
                {
                    var savedUser = new User()
                    {
                        Name = GetUserFullName(message.Message.ForwardSender),
                        UserId = userId,
                    };

                    await AddOrUpdateUserAsync(savedUser);

                    needUser = savedUser;
                }

                var mrMessage = new MergeSetting { MrUrl = mrUrl };

                var ticketRegex = new Regex(TICKET_PATTERN);

                var ticketMatches = ticketRegex.Matches(messageText);

                var description = messageText.Replace(mrUrl, string.Empty);

                foreach (Match ticketMatch in ticketMatches)
                {
                    mrMessage.TicketsUrl.Add(ticketMatch.Value);
                    description = description.Replace(ticketMatch.Value, string.Empty);
                }

                mrMessage.PublishDate = new DateTimeOffset(UnixTimeStampToDateTime((double)message.Message.ForwardDate));
                mrMessage.Id = message.Message.Id.ToString();

                var lineOfMessage = description.Split('\r').ToList();
                var firstLine = lineOfMessage.FirstOrDefault();

                if (_changesNotation.Any(c => c.Equals(firstLine, StringComparison.InvariantCultureIgnoreCase)))
                {
                    lineOfMessage.RemoveAt(0);
                    description = string.Join('\r', lineOfMessage);
                }

                mrMessage.Description = description.Trim(new char[] { '\r', '\n' });
                mrMessage.Owner = needUser;

                conversation.ListOfMerge.Add(mrMessage);

                await SaveConversationAsync(conversation);

                responseMessage.ChatId = conversation.MRChat.Id;
                responseMessage.Text = messageText;
            }

            return responseMessage;
        }

        private async Task<SendMessageRequest> SetupUsersForConversationAsync(Update message)
        {
            var convesationId = message.Message.Chat.Id.ToString();
            var userId = message.Message.ForwardSender.Id.ToString();
            var responseMessage = new SendMessageRequest()
            {
                ChatId = convesationId,
            };

            var conversation = await GetConversationByIdAsync(convesationId);

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

                    await SaveConversationAsync(conversation);
                    await AddOrUpdateUserAsync(newUser);
                }
            }
            else
            {
                responseMessage.Text = "This conversation is not MR chat!";
            }

            return responseMessage;
        }

        #endregion

        #region Skype part

        public async Task<Activity> ReturnMessage(ITurnContext turnContext)
        {
            var returnMessage = new Activity();

            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                var message = turnContext.Activity.Text;

                if (message.Equals(MARK_MR_CONVERSATION))
                {
                    return await SetupMRConversationAsync(turnContext);
                }
                else if (new Regex(GUID_PATTERN).IsMatch(message) && message.StartsWith($"{MARK_ALERT_CONVERSATION}"))
                {
                    if (Guid.TryParse(new Regex(GUID_PATTERN).Match(message).Value, out var syncId))
                        return await SetupAlertConversationAsync(turnContext, syncId);
                    else return turnContext.Activity.CreateReply();
                }
                else if (message.Equals(REMOVE_MR_CONVERSATION))
                {
                    return await RemoveMrConversationAsync(turnContext);
                }
                else if (message.Equals(REMOVE_ALERT_CONVERSATION))
                {
                    return await RemoveAlertConversationAsync(turnContext);
                }
                else if (message.Equals(SETUP_ADMIN))
                {
                    return await AddAdminAsync(turnContext);
                }
                else if (message.Equals(REMOVE_ADMIN))
                {
                    return await RemoveAdminAsync(turnContext);
                }
                else if (message.StartsWith(GET_STATISTIC))
                {
                    return await GetStatisticsAsync(turnContext);
                }
                else if (message.Equals(GET_UNMARKED_MR))
                {
                    return await GetUnMarkedMergeRequestAsync(turnContext);
                }
                else if (message.Equals(GET_MY_UNMARKED_MR))
                {
                    return await GetMyUnMarkedMergeRequestAsync(turnContext);
                }
                else
                {
                    return await GetMrMessageAsync(turnContext);
                }
            }
            else if (turnContext.Activity.Type == ActivityTypes.MessageReaction)
            {
                //
            }

            return returnMessage;
        }

        //TODO: update method
        private async Task SetReactionForMessage(ITurnContext context)
        {
            var reactions = context.Activity.ReactionsAdded.ToList();
            var users = await GetUsersAsync();
            var needUser = users.Users.FirstOrDefault(u => u.UserId.Equals(context.Activity.From.Id));

            if (needUser == null)
            {
                var savedUser = new User()
                {
                    Name = context.Activity.From.Name,
                    UserId = context.Activity.From.Id,
                };

                await AddOrUpdateUserAsync(savedUser);

                needUser = savedUser;
            }

            var messageId = context.Activity.ReplyToId;

            foreach (var messageReaction in reactions)
            {
                var type = messageReaction.Type;
            }
        }

        #region Conversation setup

        private async Task<Activity> RemoveAlertConversationAsync(ITurnContext context)
        {
            var convesationId = context.Activity.Conversation.Id;
            var userId = context.Activity.From.Id;
            var responseMessage = context.Activity.CreateReply();
            responseMessage.TextFormat = "plain";

            var users = await GetUsersAsync();
            var needUser = users.Users.FirstOrDefault(u => u.UserId.Equals(userId));
            if (needUser == null)
            {
                var savedUser = new User()
                {
                    Name = context.Activity.From.Name,
                    UserId = context.Activity.From.Id,
                };

                await AddOrUpdateUserAsync(savedUser);
            }

            var conversations = await GetCurrentConversationsAsync();
            var needConversation = conversations.BotConversation.FirstOrDefault(c => c.AlertChat.Id.Equals(convesationId));
            if (needConversation == null)
            {
                responseMessage.Text =
                    "This is not a Alert conversation or you don't add any conversation. Try in Alert conversation ;)";
            }
            else
            {
                if (needConversation.Admins.Any(u => u.UserId.Equals(userId)))
                {
                    responseMessage.Text = $"Congratulation, you remove alert conversation. Please setup alert conversation by sync id: {needConversation.MRChat.SyncId}";

                    needConversation.AlertChat = null;
                    await SaveConversationAsync(needConversation);
                }
                else
                {
                    responseMessage.Text = "You don't have permission for remove this conversation!";
                }
            }

            return responseMessage;
        }
        
        private async Task<Activity> RemoveMrConversationAsync(ITurnContext context)
        {
            var convesationId = context.Activity.Conversation.Id;
            var userId = context.Activity.From.Id;
            var responseMessage = context.Activity.CreateReply();
            responseMessage.TextFormat = "plain";

            var users = await GetUsersAsync();
            var needUser = users.Users.FirstOrDefault(u => u.UserId.Equals(userId));
            if (needUser == null)
            {
                var savedUser = new User()
                {
                    Name = context.Activity.From.Name,
                    UserId = context.Activity.From.Id,
                };

                await AddOrUpdateUserAsync(savedUser);
            }

            var conversations = await GetCurrentConversationsAsync();

            var needConversation = conversations.BotConversation.FirstOrDefault(c => c.MRChat.Id.Equals(convesationId));
            if (needConversation == null)
            {
                responseMessage.Text =
                    "This is not a MR's conversation or you don't add any conversation. Try in MR's conversation ;)";
            }
            else
            {
                if (needConversation.Admins.Any(u => u.UserId.Equals(userId)))
                {
                    responseMessage.Text = "Congratulation, you remove all data with linked for current conversation";
                    // TODO: add get stat and send before remove
                    await RemoveConversationAsync(needConversation);
                }
                else
                {
                    responseMessage.Text = "You don't have permission for remove this conversation!";
                }
            }

            return responseMessage;
        }

        private async Task<Activity> SetupMRConversationAsync(ITurnContext context)
        {
            var conversationId = context.Activity.Conversation.Id;

            var resultMessage = context.Activity.CreateReply();

            var conversations = await GetCurrentConversationsAsync();

            if (!conversations.BotConversation.Any())
            {
                var syncId = Guid.NewGuid();
                var chatSetting = new ChatSetting()
                {
                    Id = conversationId,
                    IsMRChat = true,
                    SyncId = syncId,
                    Name = context.Activity.Conversation.Name,
                    BaseActivity = context.Activity,
                };

                var newConversation = new ConversationSetting()
                {
                    MRChat = chatSetting,
                };

                await SaveConversationAsync(newConversation);

                resultMessage.Text = $"Current chat is setup as MR with sync id: {syncId}";
            }

            return resultMessage;
        }

        private async Task<Activity> SetupAlertConversationAsync(ITurnContext context, Guid syncId)
        {
            var conversationId = context.Activity.Conversation.Id;
            var responseMessage = context.Activity.CreateReply();
            responseMessage.TextFormat = "plain";

            var conversations = await GetCurrentConversationsAsync();

            if (!conversations.BotConversation.Any())
            {
                responseMessage.Text = "Have not setup any conversation to Bot :)";
            }
            else
            {
                var neededConversation =
                    conversations.BotConversation.FirstOrDefault(c => c.MRChat.SyncId.Equals(syncId));
                if (neededConversation == null)
                {
                    responseMessage.Text = $"Didn't setup MR chat for current sync id: {syncId}";
                }
                else
                {
                    neededConversation.AlertChat = new ChatSetting()
                    {
                        Id = conversationId,
                        IsAlertChat = true,
                        SyncId = syncId,
                        Name = context.Activity.Conversation.Name,
                        BaseActivity = context.Activity,
                    };

                    await SaveConversationAsync(neededConversation);
                    responseMessage.Text = $"This chat is setup as alert chat for conversation {neededConversation.MRChat.Name}";
                }
            }

            return responseMessage;
        }

        private async Task<Activity> AddAdminAsync(ITurnContext context)
        {
            var responseMessage = new Activity();

            responseMessage = await GetMessageWithCheckChatAsync(
                async (conversation) =>
            {
                if (conversation.Admins.Count >= MAX_COUNT_OF_ADMINS)
                {
                    return "Admin list for current conversation is full";
                }
                else
                {
                    var users = await GetUsersAsync();
                    var needUser = users.Users.FirstOrDefault(u => u.UserId.Equals(context.Activity.From.Id));
                    if (needUser == null)
                    {
                        var savedUser = new User()
                        {
                            Name = context.Activity.From.Name,
                            UserId = context.Activity.From.Id,
                        };

                        await AddOrUpdateUserAsync(savedUser);

                        needUser = savedUser;
                    }

                    conversation.Admins.Add(needUser);

                    await SaveConversationAsync(conversation);

                    return $"{needUser.Name} add like admin";
                }
            }, context);

            return responseMessage;
        }

        private async Task<Activity> RemoveAdminAsync(ITurnContext context)
        {
            var responseMessage = new Activity();

            responseMessage = await GetMessageWithCheckChatAsync(
                async (conversation) =>
                {
                    var users = await GetUsersAsync();
                    var needUser = users.Users.FirstOrDefault(u => u.UserId.Equals(context.Activity.From.Id));
                    if (needUser == null)
                    {
                        var savedUser = new User()
                        {
                            Name = context.Activity.From.Name,
                            UserId = context.Activity.From.Id,
                        };

                        await AddOrUpdateUserAsync(savedUser);

                        needUser = savedUser;
                    }

                    if (conversation.Admins.Any(u => u.UserId.Equals(needUser.UserId)))
                    {
                        conversation.Admins.RemoveAll(u => u.UserId.Equals(needUser.UserId));
                        return "You have been removed from admin of current conversation";
                    }

                    return $"Good joke xD You are admin of chat. Try next time ;)";
                }, context);

            return responseMessage;
        }

        #endregion

        #region Merge Requst

        private async Task<Activity> GetMrMessageAsync(ITurnContext context)
        {
            var userId = context.Activity.From.Id;
            var responseMessage = context.Activity.CreateReply(string.Empty);
            responseMessage.TextFormat = "plain";

            var messageText = context.Activity.Text;

            var mrRegex = new Regex(MR_PATTERN);
            var mrUrlMatch = mrRegex.Match(messageText);
            var mrUrl = mrUrlMatch.Value;

            var conversation = await GetConversationByIdAsync(context.Activity.Conversation.Id);

            if (await IsMrContainceAsync(mrUrl, context.Activity.Conversation.Id))
            {
                // for updatedTicket
                var needMr = conversation.ListOfMerge.FirstOrDefault(m => m.MrUrl.Equals(mrUrl));
                if (needMr == null)
                {
                    return responseMessage;
                }

                needMr.IsHadAlreadyChange = true;
                needMr.CountOfChange++;

                needMr.VersionedSetting.Add(new VersionedMergeRequest()
                {
                    OwnerMergeId = needMr.Id,
                    PublishDate = context.Activity.Timestamp ?? DateTimeOffset.UtcNow,
                    Id = context.Activity.Id,
                });

                await SaveConversationAsync(conversation);
            }
            else
            {
                var users = await GetUsersAsync();
                var needUser = users.Users?.FirstOrDefault(u => u.UserId.Equals(userId));
                if (needUser == null)
                {
                    var savedUser = new User()
                    {
                        Name = context.Activity.From.Name,
                        UserId = userId,
                    };

                    await AddOrUpdateUserAsync(savedUser);

                    needUser = savedUser;
                }

                var mrMessage = new MergeSetting { MrUrl = mrUrl };

                var ticketRegex = new Regex(TICKET_PATTERN);

                var ticketMatches = ticketRegex.Matches(messageText);

                var description = messageText.Replace(mrUrl, string.Empty);

                foreach (Match ticketMatch in ticketMatches)
                {
                    mrMessage.TicketsUrl.Add(ticketMatch.Value);
                    description = description.Replace(ticketMatch.Value, string.Empty);
                }

                mrMessage.PublishDate = context.Activity.Timestamp ?? DateTimeOffset.UtcNow;
                mrMessage.Id = context.Activity.Id;

                var lineOfMessage = description.Split('\r').ToList();
                var firstLine = lineOfMessage.FirstOrDefault();

                if (_changesNotation.Any(c => c.Equals(firstLine, StringComparison.InvariantCultureIgnoreCase)))
                {
                    lineOfMessage.RemoveAt(0);
                    description = string.Join('\r', lineOfMessage);
                }

                mrMessage.Description = description.Trim(new char[] { '\r', '\n' });
                mrMessage.Owner = needUser;

                conversation.ListOfMerge.Add(mrMessage);

                await SaveConversationAsync(conversation);
            }

            return responseMessage;
        }

        private async Task<Activity> GetUnMarkedMergeRequestAsync(ITurnContext context)
        {
            var responseMessages = await GetMessageWithCheckChatAsync(
                async (conversation) =>
                {
                    var mrs = conversation.ListOfMerge.Where(m => m.Reactions.Count < 2).ToList();

                    if (!mrs.Any())
                    {
                        return "Good job guys, you don't have any merge request which must be reviewed :)";
                    }

                    var builder = new StringBuilder();

                    builder.Append($@"---------------------------

You have {mrs.Count} unchecked merge requests
");

                    foreach (var mergeSetting in mrs)
                    {
                        builder.Append($@"\t- {mergeSetting.Owner.Name} publish MR with link {mergeSetting.MrUrl}
");
                    }

                    builder.Append(@"
Thanks :) 
---------------------------");

                    return builder.ToString();
                }, context);


            return responseMessages;
        }

        private async Task<Activity> GetMyUnMarkedMergeRequestAsync(ITurnContext context)
        {
            var userId = context.Activity.From.Id;

            var responseMessages = await GetMessageWithCheckChatAsync(
                async (conversation) =>
                {
                    var mrs = conversation.ListOfMerge.Where(m => m.Reactions.Count < 2 && m.Owner.UserId.Equals(userId)).ToList();

                    if (!mrs.Any())
                    {
                        return "Good job, you don't have any merge request which must be reviewed :)";
                    }

                    var builder = new StringBuilder();

                    builder.Append($@"---------------------------

You have {mrs.Count} unchecked merge requests
");

                    foreach (var mergeSetting in mrs)
                    {
                        builder.Append($@"\t- {mergeSetting.Owner.Name} publish MR with link {mergeSetting.MrUrl}
");
                    }

                    builder.Append(@"
Thanks :) 
---------------------------");

                    return builder.ToString();
                }, context);


            return responseMessages;
        }

        #endregion

        #region Stats

        private async Task<Activity> GetStatisticsAsync(ITurnContext context)
        {
            var responseMessage = await GetMessageWithCheckChatAsync(
                async (conversation) =>
                {
                    var message = context.Activity.Text;
                    message = message.Replace(GET_STATISTIC, string.Empty);
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

                    var result = new ResponseMessage();

                    switch (command)
                    {
                        case "getalldata":
                            result = StatHtmlBuilder.GetAllData(conversation.ListOfMerge, startDate, endDate);
                            break;
                        case "getmrreaction":
                            result = StatHtmlBuilder.GetMRReaction(conversation.ListOfMerge, startDate, endDate);
                            break;
                        case "getusermrreaction":
                            result = StatHtmlBuilder.GetUsersMRReaction(conversation.ListOfMerge, startDate, endDate);
                            break;
                        case "getunmarked":
                            result = StatHtmlBuilder.GetUnmarkedCountMergePerDay(conversation.ListOfMerge, startDate,
                                endDate);
                            break;
                    }
                    
                    return result.Message;
                }, context);

            responseMessage.TextFormat = "plain";

            responseMessage.Attachments = new List<Attachment>()
            {
                new Attachment()
                {
                    Name = "Stat",
                    ContentUrl = responseMessage.Text,
                    ContentType = DownlodController.GetContentType(responseMessage.Text),
                },
            };

            return responseMessage;
        }

        #endregion

        #endregion

        #region Helpers

        private async Task<Activity> GetMessageWithCheckChatAsync(Func<ConversationSetting, Task<string>> successAction, ITurnContext context)
#pragma warning restore SA1124 // Do not use regions
        {
            var conversationId = context.Activity.Conversation.Id;

            var activity = new Activity();

            var conversations = await GetCurrentConversationsAsync();

            if (conversations.BotConversation.Any())
            {
                var conversation =
                    conversations.BotConversation.FirstOrDefault(c => c.AlertChat.Id.Equals(conversationId));
                if (conversation == null)
                {
                    var mrConversation =
                        conversations.BotConversation.FirstOrDefault(c => c.MRChat.Id.Equals(conversationId));
                    if (mrConversation != null)
                    {
                        activity = mrConversation.MRChat.BaseActivity.CreateReply();
                        activity.Text =
                            $"This is not conversation for input command, please select conversation named {mrConversation.AlertChat?.Name ?? string.Empty}";
                        activity.TextFormat = "plain";
                    }
                    else
                    {
                        activity = context.Activity.CreateReply();
                        activity.Text = "I don't know why are you want to do something in useless conversation :(";
                        activity.TextFormat = "plain";
                    }
                }
                else
                {
                    activity = conversation.AlertChat.BaseActivity.CreateReply();
                    activity.Text = await successAction.Invoke(conversation);
                    activity.TextFormat = "plain";
                }
            }

            return activity;
        }

        private async Task<bool> IsMrContainceAsync(string mrUrl, string chatId)
        {
            if (string.IsNullOrEmpty(mrUrl) || string.IsNullOrEmpty(chatId)) return false;

            var conversations = await GetCurrentConversationsAsync();

            if (conversations?.BotConversation == null || !conversations.BotConversation.Any()) return false;

            var mrChat = conversations.BotConversation.FirstOrDefault(c => c.MRChat.Id.Equals(chatId));

            if (mrChat?.ListOfMerge == null || !mrChat.ListOfMerge.Any()) return false;

            return mrChat.ListOfMerge.Any(c => c.MrUrl.Equals(mrUrl, StringComparison.InvariantCultureIgnoreCase));
        }

        private async Task SaveConversationAsync(ConversationSetting conversation)
        {
            var conversations = await GetCurrentConversationsAsync();

            conversations.BotConversation.RemoveAll(c => c.Id.Equals(conversation.Id));

            conversations.BotConversation.Add(conversation);

            await ConversationState.Storage.WriteAsync(new Dictionary<string, object>()
            {
                {CONVERSATION_KEY, conversations},
            });
        }

        private async Task RemoveConversationAsync(ConversationSetting conversation)
        {
            var conversations = await GetCurrentConversationsAsync();

            conversations.BotConversation.RemoveAll(c => c.Id.Equals(conversation.Id));

            await ConversationState.Storage.WriteAsync(new Dictionary<string, object>()
            {
                {CONVERSATION_KEY, conversations},
            });
        }

        private async Task<ConversationSetting> GetConversationByIdAsync(string chatId)
        {
            if (string.IsNullOrEmpty(chatId)) return null;

            var conversations = await GetCurrentConversationsAsync();

            if (conversations?.BotConversation == null || !conversations.BotConversation.Any()) return null;

            return conversations.BotConversation.FirstOrDefault(c => c.MRChat.Id.Equals(chatId) || c.AlertChat.Id.Equals(chatId));
        }

        private async Task<Conversations> GetCurrentConversationsAsync()
        {
            var storeProvider = ConversationState.Storage;

            var result = await storeProvider.ReadAsync(new[] { CONVERSATION_KEY }, CancellationToken.None);

            if (result == null || !result.Any() || !result.ContainsKey(CONVERSATION_KEY))
            {
                await GetNewConversationAsync(storeProvider);
            }

            if (result.TryGetValue(CONVERSATION_KEY, out var conversations) && conversations is Conversations conversations1)
            {
                return conversations1;
            }

            return await GetNewConversationAsync(storeProvider);
        }

        private async Task<Conversations> GetNewConversationAsync(IStorage storeProvider)
        {
            var newConversations = new Conversations();

            await storeProvider.WriteAsync(
                new Dictionary<string, object>()
            {
                { CONVERSATION_KEY, newConversations },
            }, CancellationToken.None);

            return newConversations;
        }

        private async Task<UserSetting> GetUsersAsync()
        {
            var storeProvider = ConversationState.Storage;

            var result = await storeProvider.ReadAsync(new[] { USER_SETTING_KEY }, CancellationToken.None);

            if (result == null || !result.Any() || !result.ContainsKey(USER_SETTING_KEY))
            {
                await GetNewUserAsync(storeProvider);
            }

            if (result.TryGetValue(CONVERSATION_KEY, out var userObject) && userObject is UserSetting user)
            {
                return user;
            }

            return await GetNewUserAsync(storeProvider);
        }

        private async Task AddOrUpdateUserAsync(User user)
        {
            var users = await GetUsersAsync();

            if (users.Users.Any(u => u.UserId.Equals(user.UserId)))
            {
                users.Users.RemoveAll(u => u.UserId.Equals(user.UserId));
                users.Users.Add(user);
            }
            else
            {
                users.Users.Add(user);
            }

            await ConversationState.Storage.WriteAsync(new Dictionary<string, object>()
            {
                { USER_SETTING_KEY, users },
            });
        }

        private async Task<UserSetting> GetNewUserAsync(IStorage storeProvider)
        {
            var userSetting = new UserSetting();

            await storeProvider.WriteAsync(
                new Dictionary<string, object>()
                {
                    { USER_SETTING_KEY, userSetting },
                }, CancellationToken.None);

            return userSetting;
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

        private void AddButtonForRequest(SendMessageRequest message, string mrId, string mrUrl)
        {
            message.ReplyMarkup = new InlineKeyboardMarkup()
            {
                InlineKeyboardButtons = new List<List<InlineKeyboardButton>>()
                {
                    new List<InlineKeyboardButton>()
                    {
                        new InlineKeyboardButton()
                        {
                            Text = "👍",
                            CallbackData = $"/success reaction {mrId}",
                        },
                        new InlineKeyboardButton()
                        {
                            Text = "MR link",
                            Url = mrUrl,
                        },
                    },
                },
            };
        }

        #endregion

    }
}
