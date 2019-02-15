using InnovaMRBot.Commands;
using InnovaMRBot.InlineActions;
using InnovaMRBot.Models;
using InnovaMRBot.Repository;
using InnovaMRBot.SubCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using InnovaMRBot.Models.Enum;
using TelegramBotApi.Extension;
using TelegramBotApi.Models;
using TelegramBotApi.Models.Enum;
using TelegramBotApi.Models.Keyboard;
using TelegramBotApi.Telegram;
using TelegramBotApi.Telegram.Request;
using Action = InnovaMRBot.Models.Action;

namespace InnovaMRBot.Services
{
    public class ChatStateService
    {
        #region Constants

        protected const string TICKET_NUMBER_PATTERN = @"\w+[0-9]+";

        private const string MARK_MR_CONVERSATION = "/start MR chat";
        
        private const string REMOVE_MR_CONVERSATION = "/remove MR chat";
        
        private object _lockerSaveToDbObject = new object();

        private readonly List<BaseCommand> _commands;

        private readonly Dictionary<Guid, Timer> _scheduler = new Dictionary<Guid, Timer>();

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
                if (InlineAction.Actions.ContainsKey(update.CallbackQuery.Data))
                {
                    InlineAction.Actions[update.CallbackQuery.Data].Invoke(update, _telegramService, _dbContext, SchedulerAction)
                        .ConfigureAwait(false);
                }
                else
                {
                    if (update.CallbackQuery.Data.StartsWith(Glossary.InlineAction.SUCCESS_REACTION_MR))
                    {
                        //TODO: add method for like for MR
                    }
                    else if (update.CallbackQuery.Data.StartsWith(Glossary.InlineAction.BAD_REACTION_MR))
                    {
                        //TODO: add method for block for MR
                    }
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

        private void SchedulerAction(Guid id, ActionType type)
        {
            if (type == ActionType.Add)
            {
                var action = _dbContext.Actions.Get(id);

                var needTimeToStart = DateTime.UtcNow.Subtract(action.ExecDate);

                var data = new object[] { _dbContext, _telegramService, id.ToString() };

                var timer = new Timer(SchedulerActionCallBack, data, needTimeToStart, TimeSpan.FromMilliseconds(-1));

                _scheduler.Add(id, timer);
            }
            else
            {
                if (_scheduler.ContainsKey(id))
                {
                    var removeTimer = _scheduler[id];
                    removeTimer.Dispose();
                    _scheduler.Remove(id);
                }
            }
        }

        private void SchedulerActionCallBack(object state)
        {
            var getData = state as object[];

            var dbContext = getData[0] as UnitOfWork;
            var telegram = getData[1] as Telegram;
            var id = Guid.Parse(getData[2] as string);

            var action = dbContext.Actions.Get(id);
            if (!action.IsActive) return;

            var conversation = _dbContext.Conversations.GetAll().FirstOrDefault(c => c.MRChat != null);
            var merge = conversation.ListOfMerge.FirstOrDefault(m => m.TelegramMessageId == action.MessageId);

            if (merge == null)
            {
                merge = conversation.ListOfMerge.FirstOrDefault(m => m.VersionedSetting.Any(v => v.Id == action.MessageId));
            }

            switch (action.ActionMethod)
            {
                case Glossary.ActionType.UNMARKED:

                    break;
                case Glossary.ActionType.WATCH_NOTIFICATION:
                    WatchNotification(dbContext, action, telegram, merge);
                    break;
                case Glossary.ActionType.REVIEW_NOTIFICATION:
                    ReviewNotification(dbContext, action, telegram, merge);
                    break;
            }
        }

        private void WatchNotification(UnitOfWork unitOfWork, Action action, Telegram telegram, MergeSetting merge)
        {
            var owner = unitOfWork.Users.GetAll().FirstOrDefault(c => c.UserId.Equals(merge.OwnerId));

            telegram.SendMessageAsync(new SendMessageRequest()
            {
                ChatId = action.ActionFor,
                Text = $"Please mark MR number {Regex.Match(merge.MrUrl, TICKET_NUMBER_PATTERN).Value} by {owner.Name}",
                FormattingMessageType = FormattingMessageType.Markdown,
                ReplyMarkup = new InlineKeyboardMarkup()
                {
                    InlineKeyboardButtons = new List<List<InlineKeyboardButton>>()
                    {
                        new List<InlineKeyboardButton>()
                        {
                            new InlineKeyboardButton()
                            {
                                Text = "👍",
                                CallbackData = $"{Glossary.InlineAction.SUCCESS_REACTION_MR}{action.MessageId}",
                            },
                            new InlineKeyboardButton()
                            {
                                Text = "MR",
                                Url = merge.MrUrl,
                            },
                            new InlineKeyboardButton()
                            {
                                Text = "🚫",
                                CallbackData = $"{Glossary.InlineAction.BAD_REACTION_MR}{action.MessageId}",
                            },
                        },
                    },
                },
            }).ConfigureAwait(false);
        }

        private void ReviewNotification(UnitOfWork unitOfWork, Action action, Telegram telegram, MergeSetting merge)
        {
            var users = _dbContext.Users.GetAll();

            var last = GetLastVersion(merge);
            var owner = unitOfWork.Users.GetAll().FirstOrDefault(c => c.UserId.Equals(merge.OwnerId));

            var neededUsers = users.Where(u =>
                u.UserId != merge.OwnerId &&
                last.Reactions.Any(r => r.UserId == u.UserId && r.ReactionType != ReactionType.Watch)).ToList();

            foreach (var neededUser in neededUsers)
            {
                var text = $"Can you review MR number {Regex.Match(merge.MrUrl, TICKET_NUMBER_PATTERN).Value} by {owner.Name}?";
                telegram.SendMessageAsync(new SendMessageRequest()
                {
                    ChatId = neededUser.ChatId,
                    Text = text,
                    FormattingMessageType = FormattingMessageType.Markdown,
                    ReplyMarkup = new InlineKeyboardMarkup()
                    {
                        InlineKeyboardButtons = new List<List<InlineKeyboardButton>>()
                        {
                            new List<InlineKeyboardButton>()
                            {
                                new InlineKeyboardButton()
                                {
                                    Text = "👍",
                                    CallbackData = $"{Glossary.InlineAction.SUCCESS_REACTION_MR}{action.MessageId}",
                                },
                                new InlineKeyboardButton()
                                {
                                    Text = "MR",
                                    Url = merge.MrUrl,
                                },
                                new InlineKeyboardButton()
                                {
                                    Text = "🚫",
                                    CallbackData = $"{Glossary.InlineAction.BAD_REACTION_MR}{action.MessageId}",
                                },
                            },
                        },
                    },
                }).ConfigureAwait(false);
            }
        }

        private static VersionedMergeRequest GetLastVersion(MergeSetting merge)
        {
            var result = new VersionedMergeRequest()
            {
                PublishDate = merge.PublishDate,
                Reactions = merge.Reactions.Where(r => r.ReactionType == ReactionType.Like).ToList(),
            };

            if (merge.VersionedSetting != null && merge.VersionedSetting.Any())
            {
                return merge.VersionedSetting.FirstOrDefault(c =>
                    c.PublishDate == merge.VersionedSetting.Max(m => m.PublishDate));
            }

            return result;
        }

        #region Telegram part

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

            var conversations = _dbContext.Conversations.GetAll();

            var needConversation = conversations.FirstOrDefault(c => c.MRChat.Id.Equals(convesationId));
            if (needConversation == null)
            {
                responseMessage.Text =
                    "This is not a MR's conversation or you don't add any conversation. Try in MR's conversation ;)";
            }

            return responseMessage;
        }

        #endregion
    }
}
