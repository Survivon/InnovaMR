
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InnovaMRBot.Helpers;
using InnovaMRBot.Models;
using InnovaMRBot.Repository;
using InnovaMRBot.SubCommand;
using TelegramBotApi.Extension;
using TelegramBotApi.Models;
using TelegramBotApi.Models.Enum;
using TelegramBotApi.Models.Keyboard;
using TelegramBotApi.Telegram;
using TelegramBotApi.Telegram.Request;

namespace InnovaMRBot.Commands
{
    public class SprintCommand : BaseCommand
    {
        private const string COMMAND = "/sprint";
        private const string ADD_ACTION = "add";
        private const string EDIT_ACTION = "update";
        private const string REMOVE_ACTION = "delete";

        public const string COMMANDID = "sprintmaincommand";

        private readonly Dictionary<string, BaseCommand> _subCommand = new Dictionary<string, BaseCommand>();

        public SprintCommand(Telegram telegram, UnitOfWork dbContext) : base(telegram, dbContext)
        {
            CommandId = COMMANDID;
            _subCommand = new Dictionary<string, BaseCommand>()
            {
                { Glossary.Sprint.ADD_ACTION, new SprintAddActionSubCommand(telegram, dbContext) },
                { Glossary.Sprint.UPDATE_ACTION, new SprintUpdateActionSubCommand(telegram, dbContext) },
                { Glossary.Sprint.REMOVE_ACTION, new SprintRemoveActionSubCommand(telegram, dbContext) },
            };
        }

        public override bool IsThisCommand(string message)
        {
            return message.Equals(COMMAND) || message.Equals(COMMANDID);
        }

        public override async Task WorkerAsync(Update update)
        {
            var userId = update.Message.Sender.Id.ToString();
            var message = update.Message.Text;

            if (message.Equals(COMMAND))
            {
                UpdateCommand(userId, CommandId, string.Empty);

                var requestWithCommandMessage = new SendMessageRequest
                {
                    ChatId = update.Message.Chat.Id.ToString(),
                    Text = "Please select action for work with sprint",
                    FormattingMessageType = FormattingMessageType.Markdown,
                    ReplyMarkup = new ReplyKeyboardMarkup()
                    {
                        IsHideKeyboardAfterClick = true,
                        Keyboard = new List<List<KeyboardButton>>()
                        {
                            new List<KeyboardButton>(_subCommand.Keys.Select(k => new KeyboardButton { Text = k })),
                        },
                    },
                };

                _telegram.SendMessageAsync(requestWithCommandMessage).ConfigureAwait(false);
            }
            else
            {
                var responseMessage = string.Empty;

                message = message.Replace(COMMAND, string.Empty).Trim(' ');

                var keywords = message.Split(' ');

                if (keywords.Length > 1)
                {
                    var conversations = _dbContext.Conversations.GetAll();
                    var conversation = conversations.FirstOrDefault(c => c.MRChat != null);

                    var sprints = conversation.MRChat.Sprints ?? new List<Sprint>();
                    var number = Convert.ToInt32(keywords[1]);

                    switch (keywords[0])
                    {
                        case ADD_ACTION:
                            responseMessage = AddAction(sprints, conversation, number, keywords);
                            break;
                        case EDIT_ACTION:
                            responseMessage = EditAction(sprints, conversation, number, keywords);
                            break;
                        case REMOVE_ACTION:
                            responseMessage = RemoveAction(sprints, conversation, number, keywords);
                            break;
                    }
                }
                else
                {
                    responseMessage =
                        "Need to add _`command(add, update, delete)` `sprint number` `start date` `end date`_";
                }

                var requestMessage = new SendMessageRequest
                {
                    ChatId = update.Message.Chat.Id.ToString(),
                    Text = responseMessage,
                    FormattingMessageType = FormattingMessageType.Markdown,
                    ReplyMarkup = new ReplyKeyboardHide() { IsHideKeyboard = true },
                };

                _telegram.SendMessageAsync(requestMessage).ConfigureAwait(false);
            }

            _dbContext.Save();
        }

        public override async Task WorkOnAnswerAsync(Update update)
        {
            UpdateCommand(update.Message.Sender.Id.ToString(), CommandId, update.Message.Text);

            var answer = update.Message.Text;

            if (_subCommand.ContainsKey(answer))
            {
                _subCommand[answer].WorkerAsync(update).ConfigureAwait(false);
            }
        }

        #region Actions

        private string AddAction(List<Sprint> sprints, ConversationSetting conversation, int number, string[] keywords)
        {
            var responseMessage = string.Empty;

            if (keywords.Length > 3)
            {
                try
                {
                    var start = keywords[2].ConvertToDate();
                    var end = keywords[3].ConvertToDate();

                    sprints.Add(new Sprint
                    {
                        End = end,
                        Number = number,
                        Start = start,
                    });
                    responseMessage = "Sprint save!";
                    _dbContext.Conversations.Update(conversation);
                }
                catch (Exception)
                {
                    responseMessage = "Incorrect date input try format M/d/yyyy";
                }
            }
            else
            {
                responseMessage = "Need add date info start than end";
            }

            return responseMessage;
        }

        private string EditAction(List<Sprint> sprints, ConversationSetting conversation, int number, string[] keywords)
        {
            var responseMessage = string.Empty;
            if (sprints.Any())
            {
                var sprint = sprints.FirstOrDefault(s => s.Number == number);

                if (sprint != null)
                {
                    try
                    {
                        var start = keywords[2].ConvertToDate();
                        var end = keywords[3].ConvertToDate();
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

            return responseMessage;
        }

        private string RemoveAction(List<Sprint> sprints, ConversationSetting conversation, int number, string[] keywords)
        {
            var responseMessage = string.Empty;
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

            return responseMessage;
        }
        
        #endregion
    }
}
