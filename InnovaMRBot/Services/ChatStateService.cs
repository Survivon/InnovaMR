using InnovaMRBot.Commands;
using InnovaMRBot.InlineCommands;
using InnovaMRBot.Models;
using InnovaMRBot.Models.Enum;
using InnovaMRBot.Repository;
using InnovaMRBot.SubCommand;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
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

        private readonly List<BaseInlineCommand> _inlineCommand;

        private readonly Dictionary<Guid, Timer> _scheduler = new Dictionary<Guid, Timer>();

        #endregion

        private readonly Telegram _telegramService;

        private readonly UnitOfWork _dbContext;

        private readonly Logger _logger;

        public ChatStateService(Telegram telegram, UnitOfWork dbContext, Logger logger)
        {
            _telegramService = telegram;
            _dbContext = dbContext;
            _logger = logger;

            _commands = new List<BaseCommand>()
            {
                new CommonDocumentCommand(_telegramService, _dbContext, _logger),
                new HelpCommand(_telegramService, _dbContext, _logger),
                new MergeRequestCommand(_telegramService, _dbContext, _logger),
                new StartCommand(_telegramService, _dbContext, _logger),

                new SprintCommand(_telegramService, _dbContext, _logger),
                new SprintAddActionSubCommand(_telegramService, _dbContext, _logger),
                new SprintAddDateActionSubCommand(_telegramService, _dbContext, _logger),
                new SprintUpdateActionSubCommand(_telegramService, _dbContext, _logger),
                new SprintUpdateDateActionSubCommand(_telegramService, _dbContext, _logger),
                new SprintRemoveActionSubCommand(_telegramService, _dbContext, _logger),

                new GetStatisticCommand(_telegramService, _dbContext, _logger),
                new GetStatisticAllActionSubCommand(_telegramService, _dbContext, _logger),
                new GetStatisticSprintActionSubCommand(_telegramService, _dbContext, _logger),
                new GetStatisticDateActionSubCommand(_telegramService, _dbContext, _logger),

                new EditCommand(_telegramService, _dbContext, _logger),
                new EditMergeNumberActionSubCommand(_telegramService, _dbContext, _logger),

                new ClearCommand(_telegramService, _dbContext, _logger),

                new ChangeUserRoleCommand(_telegramService, _dbContext, _logger),
                new UserSettingRemoveOldMergeCommand(_telegramService, _dbContext, _logger),
                new UserSettingTimeZoneSetupCommand(_telegramService, _dbContext, _logger),
            };

            _inlineCommand = new List<BaseInlineCommand>()
            {
                new BadReactionInlineCommand(_telegramService, _dbContext, SchedulerAction, _logger),
                new MergeStatisticInlineCommand(_telegramService, _dbContext, SchedulerAction, _logger),
                new PositiveReactionInlineCommand(_telegramService, _dbContext, SchedulerAction, _logger),
                new WatchInlineCommand(_telegramService, _dbContext, SchedulerAction, _logger),
                new EditInlineCommand(_telegramService, _dbContext, SchedulerAction, _logger),
            };

            //Init MRs unmarked
            InitAction();
        }

        public async Task GetUpdateFromTelegramAsync(Update update)
        {
            if (update.Message != null)
            {
                var message = update.Message.Text;
                var userId = update.Message.Sender.Id.ToString();
                _logger.Info("GetUpdateFromTelegramAsync - Message - Start", userId);

                var user = _dbContext.Users.GetAll().FirstOrDefault(u => u.UserId.Equals(userId));

                if (message.Equals(ClearCommand.COMMAND))
                {
                    new ClearCommand(_telegramService, _dbContext, _logger).WorkerAsync(update).ConfigureAwait(false);
                }
                else if (user != null && user.Commands.Any())
                {
                    var lastCommand = user.Commands.LastOrDefault();

                    var command = _commands.FirstOrDefault(c => c.IsThisCommand(lastCommand.Command));
                    command?.WorkOnAnswerAsync(update).ConfigureAwait(false);
                }
                else
                {
                    var command = _commands.FirstOrDefault(c => c.IsThisCommand(message));

                    if (command is MergeRequestCommand mergeCommand)
                    {
                        mergeCommand.SetChatStateService(this);
                    }

                    command?.WorkerAsync(update).ConfigureAwait(false);
                }

                _logger.Info("GetUpdateFromTelegramAsync - Message - End", userId);
            }
            else if (update.ChanelMessage != null)
            {
                _logger.Info("GetUpdateFromTelegramAsync - ChanelMessage - Start", update.ChanelMessage.Sender.Id.ToString());

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

                _logger.Info("GetUpdateFromTelegramAsync - ChanelMessage - End", update.ChanelMessage.Sender.Id.ToString());
            }
            else if (update.CallbackQuery != null)
            {
                _logger.Info("GetUpdateFromTelegramAsync - CallbackQuery - Start", update.CallbackQuery.Sender.Id.ToString());
                var command = _inlineCommand.FirstOrDefault(c => c.IsThisInlineCommand(update.CallbackQuery.Data));

                command?.WorkerAsync(update, string.Empty);
                _logger.Info("GetUpdateFromTelegramAsync - CallbackQuery - End", update.CallbackQuery.Sender.Id.ToString());
            }
            else if (update.InlineQuery != null)
            {
                _logger.Info("GetUpdateFromTelegramAsync - InlineQuery - Start", update.InlineQuery.Sender.Id.ToString());
            }
            else if (update.InlineResult != null)
            {
                _logger.Info("GetUpdateFromTelegramAsync - InlineResult - Start", update.InlineResult.Sender.Id.ToString());
            }

            lock (_lockerSaveToDbObject)
            {
                _dbContext.Save();
            }
        }

        public void SchedulerAction(Guid id, DateTime execDate, ActionType type)
        {
            _logger.Info($"SchedulerAction Start with {type.ToString()}", "system");

            if (type == ActionType.Add)
            {
                var needTimeToStart = execDate.Subtract(DateTime.UtcNow);

                var data = new object[] { _dbContext, _telegramService, id.ToString() };

                if (needTimeToStart < TimeSpan.Zero) return;

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

            _logger.Info($"SchedulerAction End with {type.ToString()}", "system");
        }

        private void SchedulerActionCallBack(object state)
        {
            _logger.Info($"SchedulerActionCallBack Start", "system");

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
                    UnmarkedNotification(dbContext, action, telegram);
                    break;
                case Glossary.ActionType.WATCH_NOTIFICATION:
                    WatchNotification(dbContext, action, telegram, merge);
                    break;
                case Glossary.ActionType.REVIEW_NOTIFICATION:
                    ReviewNotification(dbContext, action, telegram, merge);
                    break;
                case Glossary.ActionType.CLEAR_TEMP_DATA:
                    ClearTempData(dbContext, action);
                    break;
            }

            _logger.Info($"SchedulerActionCallBack End", "system");
        }

        private void WatchNotification(UnitOfWork unitOfWork, Action action, Telegram telegram, MergeSetting merge)
        {
            _logger.Info($"WatchNotification Start", "system");

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

            action.IsActive = false;

            unitOfWork.Actions.Update(action);
            unitOfWork.Save();

            _logger.Info($"WatchNotification End", "system");
        }

        private void ReviewNotification(UnitOfWork unitOfWork, Action action, Telegram telegram, MergeSetting merge)
        {
            _logger.Info($"ReviewNotification Start", "system");

            var users = _dbContext.Users.GetAll();

            var last = GetLastVersion(merge);
            var owner = unitOfWork.Users.GetAll().FirstOrDefault(c => c.UserId.Equals(merge.OwnerId));

            var neededUsers = users.Where(u => u.UserId != merge.OwnerId)
                .Where(u => last.Reactions.All(r => r.UserId != u.UserId) && u.Role == UserRole.Dev).ToList();

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

            action.IsActive = false;

            unitOfWork.Actions.Update(action);
            unitOfWork.Save();

            _logger.Info($"ReviewNotification End", "system");
        }

        private void ClearTempData(UnitOfWork unitOfWork, Action action)
        {
            _logger.Info($"ClearTempData Start", "system");

            var directoryPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "temp");

            if (Directory.Exists(directoryPath))
            {
                var di = new DirectoryInfo(directoryPath);

                foreach (var file in di.GetFiles())
                {
                    file.Delete();
                }

                foreach (var dir in di.GetDirectories())
                {
                    dir.Delete(true);
                }
            }

            action.IsActive = false;
            unitOfWork.Actions.Update(action);

            var needTime = DateTime.UtcNow.AddHours(5);

            var clearAction = new Action()
            {
                Name = "clear",
                Id = Guid.NewGuid(),
                ActionMethod = Glossary.ActionType.CLEAR_TEMP_DATA,
                ExecDate = needTime,
                IsActive = true,
            };

            unitOfWork.Actions.Create(clearAction);
            unitOfWork.Save();

            SchedulerAction(clearAction.Id, clearAction.ExecDate, ActionType.Add);

            _logger.Info($"ClearTempData End", "system");
        }

        private void InitAction()
        {
            _logger.Info($"InitAction Start", "system");

            var actions = _dbContext.Actions.GetAll();

            if (actions.Any(a => a.IsActive) && !_scheduler.Any())
            {
                foreach (var action in actions.Where(a => a.IsActive))
                {
                    SchedulerAction(action.Id, action.ExecDate, ActionType.Add);
                }
            }

            var dbUnmarkedAction = actions.FirstOrDefault(a => a.ActionMethod.Equals(Glossary.ActionType.UNMARKED) && a.IsActive && a.ExecDate > DateTime.UtcNow);
            if (dbUnmarkedAction == null)
            {
#if DEBUG
                var needTimeTime = DateTime.UtcNow.AddMinutes(1);
#else
                var needTimeTime = DateTime.UtcNow;
                if (needTimeTime.Hour >= 15)
                {
                    needTimeTime = new DateTime(needTimeTime.Year, needTimeTime.Month, needTimeTime.Day + 1, 15, 0, 0);
                }
                else
                {
                    needTimeTime = new DateTime(needTimeTime.Year, needTimeTime.Month, needTimeTime.Day, 15, 0, 0);
                }
#endif

                var unmarkedAction = new Action()
                {
                    Name = "unmarked",
                    Id = Guid.NewGuid(),
                    ActionMethod = Glossary.ActionType.UNMARKED,
                    ExecDate = needTimeTime,
                    IsActive = true,
                };

                _dbContext.Actions.Create(unmarkedAction);
                _dbContext.Save();

                SchedulerAction(unmarkedAction.Id, unmarkedAction.ExecDate, ActionType.Add);
            }

            var dbClearTempDataAction = actions.FirstOrDefault(a => a.ActionMethod.Equals(Glossary.ActionType.CLEAR_TEMP_DATA) && a.IsActive && a.ExecDate > DateTime.UtcNow);
            if (dbClearTempDataAction == null)
            {
                var needTime = DateTime.UtcNow.AddHours(5);

                var clearAction = new Action()
                {
                    Name = "clear",
                    Id = Guid.NewGuid(),
                    ActionMethod = Glossary.ActionType.CLEAR_TEMP_DATA,
                    ExecDate = needTime,
                    IsActive = true,
                };

                _dbContext.Actions.Create(clearAction);
                _dbContext.Save();

                SchedulerAction(clearAction.Id, clearAction.ExecDate, ActionType.Add);
            }

            _logger.Info($"InitAction End", "system");
        }

        private void UnmarkedNotification(UnitOfWork unitOfWork, Action action, Telegram telegram)
        {
            _logger.Info($"UnmarkedNotification Start", "system");

            var conv = unitOfWork.Conversations.GetAll();

            var needConversation = conv.FirstOrDefault();

            var merges = needConversation.ListOfMerge.ToList();

            MapUsers(merges, unitOfWork.Users.GetAll().ToList());

            var needMR = new List<Tuple<string, string, int>>();

            foreach (var merge in merges)
            {
                var lastVersion = GetLastVersion(merge);

                if (lastVersion.Reactions.Count(c => c.ReactionType == ReactionType.Like) < 2)
                {
                    needMR.Add(new Tuple<string, string, int>(merge.Owner.Name, merge.MrUrl, 2 - lastVersion.Reactions.Count));
                }
            }

            if (!needMR.Any()) return;

            var resultBuilder = new StringBuilder();

            resultBuilder.AppendLine("<b>UNMARKED MergeRequests</b>");

            foreach (var tuple in needMR)
            {
                resultBuilder.AppendLine($"MR: {tuple.Item2} by <i>{tuple.Item1}</i>");
            }

            resultBuilder.Append("\n\r");

            var mrIconPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "MRBigIcon.png");

            telegram.SendPhotoAsync(new SendPhotoRequest()
            {
                Caption = resultBuilder.ToString(),
                ChatId = needConversation.MRChat.Id,
                FormattingMessageType = FormattingMessageType.HTML,
            }, mrIconPath).ConfigureAwait(false);

            action.IsActive = false;

            unitOfWork.Actions.Update(action);

            var needTimeTime = DateTime.UtcNow;

            if (needTimeTime.Hour >= 15)
            {
                needTimeTime = new DateTime(needTimeTime.Year, needTimeTime.Month, needTimeTime.Day + 1, 15, 0, 0);
            }
            else
            {
                needTimeTime = new DateTime(needTimeTime.Year, needTimeTime.Month, needTimeTime.Day, 15, 0, 0);
            }

            var unmarkedAction = new Action()
            {
                Name = "unmarked",
                Id = Guid.NewGuid(),
                ActionMethod = Glossary.ActionType.UNMARKED,
                ExecDate = needTimeTime,
                IsActive = true,
            };

            unitOfWork.Actions.Create(unmarkedAction);
            unitOfWork.Save();

            SchedulerAction(unmarkedAction.Id, unmarkedAction.ExecDate, ActionType.Add);

            _logger.Info($"UnmarkedNotification End", "system");
        }

        private static void MapUsers(List<MergeSetting> merge, List<Models.User> usersList)
        {
            if (usersList == null || !usersList.Any()) return;

            foreach (var mergeSetting in merge)
            {
                mergeSetting.Owner = usersList.FirstOrDefault(u => u.UserId.Equals(mergeSetting.OwnerId));
                foreach (var versionedMergeRequest in mergeSetting.VersionedSetting)
                {
                    foreach (var reaction in versionedMergeRequest.Reactions)
                    {
                        reaction.User = usersList.FirstOrDefault(u => u.UserId.Equals(reaction.UserId));
                    }
                }

                foreach (var mergeSettingReaction in mergeSetting.Reactions)
                {
                    mergeSettingReaction.User = usersList.FirstOrDefault(u => u.UserId.Equals(mergeSettingReaction.UserId));
                }
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
