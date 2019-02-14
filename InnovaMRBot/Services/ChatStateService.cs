using InnovaMRBot.Commands;
using InnovaMRBot.InlineActions;
using InnovaMRBot.Models;
using InnovaMRBot.Repository;
using InnovaMRBot.SubCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TelegramBotApi.Extension;
using TelegramBotApi.Models;
using TelegramBotApi.Telegram;
using TelegramBotApi.Telegram.Request;

namespace InnovaMRBot.Services
{
    public class ChatStateService
    {
        #region Constants

        private const string MARK_MR_CONVERSATION = "/start MR chat";
        
        private const string REMOVE_MR_CONVERSATION = "/remove MR chat";
        
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
                if (InlineAction.Actions.ContainsKey(update.CallbackQuery.Data))
                {
                    InlineAction.Actions[update.CallbackQuery.Data].Invoke(update, _telegramService, _dbContext)
                        .ConfigureAwait(false);
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
