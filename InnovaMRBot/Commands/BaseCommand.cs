using InnovaMRBot.Helpers;
using InnovaMRBot.Models;
using InnovaMRBot.Repository;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TelegramBotApi.Models;
using TelegramBotApi.Models.Keyboard;
using TelegramBotApi.Telegram;
using TelegramBotApi.Telegram.Request;
using User = InnovaMRBot.Models.User;

namespace InnovaMRBot.Commands
{
    public abstract class BaseCommand
    {
        protected const string TICKET_NUMBER_PATTERN = @"\w+[0-9]+";

        protected const string TICKET_PATTERN = @"https?:\/\/fortia.atlassian.net\/browse\/\w+-[0-9]+";

        protected readonly Telegram _telegram;

        protected readonly UnitOfWork _dbContext;

        public string CommandId { get; protected set; }

        protected BaseCommand(Telegram telegram, UnitOfWork dbContext)
        {
            _telegram = telegram;
            _dbContext = dbContext;
        }

        public virtual bool IsThisCommand(string message)
        {
            var result = false;

            switch (GetType())
            {
                case EqualType.StartWith:
                    return message.StartsWith(GetCommandString());
                case EqualType.Equal:
                    return message.Equals(GetCommandString());
                case EqualType.Contain:
                    return message.Contains(GetCommandString());
                case EqualType.Pattern:
                    return !string.IsNullOrEmpty(GetPattern()) && new Regex(GetPattern()).IsMatch(message);
            }

            return result;
        }

        protected virtual EqualType GetType() => EqualType.Equal;

        protected virtual string GetCommandString() => CommandId;

        public abstract Task WorkerAsync(Update update);

        protected virtual string GetPattern() => string.Empty;

        public virtual async Task WorkOnAnswerAsync(Update update) { }

        protected string GetUserId(Update update)
        {
            return update.Message.Sender.Id.ToString();
        }

        protected void UpdateCommand(string userId, string command, string answer)
        {
            var user = _dbContext.Users.GetAll().FirstOrDefault(u => u.UserId.Equals(userId));

            user.Commands.RemoveAll(c => c.Command.Equals(command));

            user.Commands.Add(new CommandCollection()
            {
                Command = command,
                Answer = answer,
            });

            _dbContext.Save();
        }

        protected void RemoveLastCommand(string userId)
        {
            var user = _dbContext.Users.GetAll().FirstOrDefault(u => u.UserId.Equals(userId));

            user.Commands.Remove(user.Commands.LastOrDefault());

            _dbContext.Save();
        }

        protected List<CommandCollection> GetCommand(string userId)
        {
            var user = _dbContext.Users.GetAll().FirstOrDefault(u => u.UserId.Equals(userId));

            return user.Commands;
        }

        protected void ClearCommands(string userId)
        {
            var user = _dbContext.Users.GetAll().FirstOrDefault(u => u.UserId.Equals(userId));

            user.Commands = new List<CommandCollection>();

            _dbContext.Users.Update(user);
            _dbContext.Save();
        }

        //TODO: replace to other file
        #region Helpers

        protected User SaveIfNeedUser(TelegramBotApi.Models.User user)
        {
            var users = _dbContext.Users.GetAll();
            var needUser = users.FirstOrDefault(u => u.UserId.Equals(user.Id.ToString()));
            if (needUser == null)
            {
                var savedUser = new User
                {
                    Name = user.GetUserFullName(),
                    UserId = user.Id.ToString(),
                };

                AddOrUpdateUser(savedUser);

                needUser = savedUser;
            }

            return needUser;
        }

        protected void AddOrUpdateUser(User user, bool isNeedUpdate = true)
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

            _dbContext.Save();
        }

        #endregion
    }
}
