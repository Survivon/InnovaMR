using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using InnovaMRBot.Helpers;
using InnovaMRBot.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace InnovaMRBot.Services
{
    public class ChatStateService
    {
        #region Constants

        private const string BOT_NAME = "InnovaBot";

        private const string MARK_MR_CONVERSATION = "/start MR chat";

        private const string MARK_ALERT_CONVERSATION = "/start alert chat";

        private const string REMOVE_MR_CONVERSATION = "/remove MR chat";

        private const string REMOVE_ALERT_CONVERSATION = "/remove alert chat";

        private const string SETUP_ADMIN = "/setup admin";

        private const string REMOVE_ADMIN = "/remove admin";

        private const string GET_STATISTIC = "/get stat";

        private const string GET_UNMARKED_MR = "/get MR";

        private const string GET_MY_UNMARKED_MR = "/get my MR";

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

        public CustomConversationState ConversationState { get; }

        public ChatStateService(CustomConversationState conversationState)
        {
            ConversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
        }

        public async Task<ResponseMessage> ReturnMessage(ITurnContext turnContext)
        {
            var returnMessage = new ResponseMessage();

            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                var message = turnContext.Activity.Text;

                if (message.Equals($"{BOT_NAME} {MARK_MR_CONVERSATION}"))
                {
                    return await SetupMRConversationAsync(turnContext);
                }
                else if (new Regex(GUID_PATTERN).IsMatch(message) && message.StartsWith($"{BOT_NAME} {MARK_ALERT_CONVERSATION}"))
                {
                    if (Guid.TryParse(new Regex(GUID_PATTERN).Match(message).Value, out var syncId))
                        return await SetupAlertConversationAsync(turnContext, syncId);
                    else return new ResponseMessage();
                }
                else if(message.Equals($"{BOT_NAME} {REMOVE_MR_CONVERSATION}"))
                {
                    return await RemoveMrConversationAsync(turnContext);
                }
                else if (message.Equals($"{BOT_NAME} {REMOVE_ALERT_CONVERSATION}"))
                {
                    return await RemoveAlertConversationAsync(turnContext);
                }
                else if (message.Equals($"{BOT_NAME} {SETUP_ADMIN}"))
                {
                    return await AddAdminAsync(turnContext);
                }
                else if (message.Equals($"{BOT_NAME} {REMOVE_ADMIN}"))
                {
                    return await RemoveAdminAsync(turnContext);
                }
                else if(message.StartsWith($"{BOT_NAME} {GET_STATISTIC}"))
                {
                    
                    return new ResponseMessage();
                }
                else if(message.Equals($"{BOT_NAME} {GET_UNMARKED_MR}"))
                {
                    return await GetUnMarkedMergeRequestAsync(turnContext);
                }
                else if(message.Equals($"{BOT_NAME} {GET_MY_UNMARKED_MR}"))
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

        private async Task<ResponseMessage> RemoveAlertConversationAsync(ITurnContext context)
        {
            var convesationId = context.Activity.Conversation.Id;
            var userId = context.Activity.From.Id;
            var responseMessage = new ResponseMessage()
            {
                ConversationId = new ChatSetting()
                {
                    Id = convesationId,
                },
            };

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
                responseMessage.Message =
                    "This is not a Alert conversation or you don't add any conversation. Try in Alert conversation ;)";
            }
            else
            {
                if (needConversation.Admins.Any(u => u.UserId.Equals(userId)))
                {
                    responseMessage.Message = $"Congratulation, you remove alert conversation. Please setup alert conversation by sync id: {needConversation.MRChat.SyncId}";

                    needConversation.AlertChat = null;
                    await SaveConversationAsync(needConversation);
                }
                else
                {
                    responseMessage.Message = "You don't have permission for remove this conversation!";
                }
            }

            return responseMessage;
        }

        private async Task<ResponseMessage> RemoveMrConversationAsync(ITurnContext context)
        {
            var convesationId = context.Activity.Conversation.Id;
            var userId = context.Activity.From.Id;
            var responseMessage = new ResponseMessage()
            {
                ConversationId = new ChatSetting()
                {
                    Id = convesationId,
                },
            };

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
                responseMessage.Message =
                    "This is not a MR's conversation or you don't add any conversation. Try in MR's conversation ;)";
            }
            else
            {
                if (needConversation.Admins.Any(u => u.UserId.Equals(userId)))
                {
                    responseMessage.Message = "Congratulation, you remove all data with linked for current conversation";
                    // TODO: add get stat and send before remove
                    await RemoveConversationAsync(needConversation);
                }
                else
                {
                    responseMessage.Message = "You don't have permission for remove this conversation!";
                }
            }

            return responseMessage;
        }

        private async Task<ResponseMessage> SetupMRConversationAsync(ITurnContext context)
        {
            var conversationId = context.Activity.Conversation.Id;

            var resultMessage = new ResponseMessage()
            {
                ConversationId = new ChatSetting()
                {
                    Id = conversationId,
                },
            };

            //var isGroup = context.Activity.Conversation.IsGroup;

            //if (!isGroup.HasValue || !isGroup.Value)
            //{
            //    return "This is not group conversation";
            //}

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
                };

                var newConversation = new ConversationSetting()
                {
                    MRChat = chatSetting,
                };

                await SaveConversationAsync(newConversation);

                resultMessage.Message = $"Current chat is setup as MR with sync id: {syncId}";
            }

            return resultMessage;
        }

        private async Task<ResponseMessage> SetupAlertConversationAsync(ITurnContext context, Guid syncId)
        {
            var conversationId = context.Activity.Conversation.Id;
            var responseMessage = new ResponseMessage()
            {
                ConversationId = new ChatSetting()
                {
                    Id = conversationId,
                },
            };

            var conversations = await GetCurrentConversationsAsync();

            if (!conversations.BotConversation.Any())
            {
                responseMessage.Message = "Have not setup any conversation to Bot :)";
            }
            else
            {
                var neededConversation =
                    conversations.BotConversation.FirstOrDefault(c => c.MRChat.SyncId.Equals(syncId));
                if (neededConversation == null)
                {
                    responseMessage.Message = $"Didn't setup MR chat for current sync id: {syncId}";
                }
                else
                {
                    neededConversation.AlertChat = new ChatSetting()
                    {
                        Id = conversationId,
                        IsAlertChat = true,
                        SyncId = syncId,
                        Name = context.Activity.Conversation.Name,
                    };

                    await SaveConversationAsync(neededConversation);
                    responseMessage.Message = $"This chat is setup as alert chat for conversation {neededConversation.MRChat.Name}";
                }
            }

            return responseMessage;
        }

        private async Task<ResponseMessage> AddAdminAsync(ITurnContext context)
        {
            var conversationId = context.Activity.Conversation.Id;
            var responseMessage = new ResponseMessage()
            {
                ConversationId = new ChatSetting()
                {
                    Id = conversationId,
                },
            };

            var message = await GetMessageWithCheckChatAsync(
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

            responseMessage.Message = message;

            return responseMessage;
        }

        private async Task<ResponseMessage> RemoveAdminAsync(ITurnContext context)
        {
            var conversationId = context.Activity.Conversation.Id;
            var responseMessage = new ResponseMessage()
            {
                ConversationId = new ChatSetting()
                {
                    Id = conversationId,
                },
            };

            var message = await GetMessageWithCheckChatAsync(
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

                    return $"Good joke xD You are admin of chat. Try next time";
                }, context);

            responseMessage.Message = message;

            return responseMessage;
        }

        #endregion

        #region Merge Requst

        private async Task<ResponseMessage> GetMrMessageAsync(ITurnContext context)
        {
            var userId = context.Activity.From.Id;
            var conversationId = context.Activity.Conversation.Id;
            var responseMessage = new ResponseMessage()
            {
                ConversationId = new ChatSetting()
                {
                    Id = conversationId,
                },
            };

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

                mrMessage.PublishDate = context.Activity.Timestamp;

                var lineOfMessage = description.Split('\r').ToList();
                var firstLine = lineOfMessage.FirstOrDefault();

                if (_changesNotation.Any(c => c.Equals(firstLine, StringComparison.InvariantCultureIgnoreCase)))
                {
                    lineOfMessage.RemoveAt(0);
                    description = string.Join('\r', lineOfMessage);
                }

                mrMessage.Description = description.Trim(new char[]{'\r','\n'});
                mrMessage.Owner = needUser;

                conversation.ListOfMerge.Add(mrMessage);

                await SaveConversationAsync(conversation);
            }

            return responseMessage;
        }

        private async Task<ResponseMessage> GetUnMarkedMergeRequestAsync(ITurnContext context)
        {
            var responseMessages = new ResponseMessage()
            {
                ConversationId = new ChatSetting()
                {
                    Id = context.Activity.Conversation.Id,
                },
            };

            var message = await GetMessageWithCheckChatAsync(
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

            responseMessages.Message = message;
            return responseMessages;
        }

        private async Task<ResponseMessage> GetMyUnMarkedMergeRequestAsync(ITurnContext context)
        {
            var userId = context.Activity.From.Id;

            var responseMessages = new ResponseMessage()
            {
                ConversationId = new ChatSetting()
                {
                    Id = context.Activity.Conversation.Id,
                },
            };

            var message = await GetMessageWithCheckChatAsync(
                async (conversation) =>
                {
                    var mrs = conversation.ListOfMerge.Where(m => m.Reactions.Count < 2 && m.Owner.UserId.Equals(userId)).ToList();

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

            responseMessages.Message = message;
            return responseMessages;
        }

        #endregion

        #region Stats



        #endregion

        #region Helpers

        private async Task<string> GetMessageWithCheckChatAsync(Func<ConversationSetting, Task<string>> successAction, ITurnContext context)
        {
            var conversationId = context.Activity.Conversation.Id;

            var results = string.Empty;

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
                        results =
                            $"This is not conversation for input command, please select conversation named {mrConversation.AlertChat?.Name ?? string.Empty}";
                    }
                    else
                    {
                        results = "I don't know why are you want to do something in useless conversation :(";
                    }
                }
                else
                {
                    results = await successAction.Invoke(conversation);
                }
            }

            return results;
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

        #endregion

    }
}
