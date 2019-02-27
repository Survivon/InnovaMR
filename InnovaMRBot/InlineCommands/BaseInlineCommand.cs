using InnovaMRBot.Models.Enum;
using InnovaMRBot.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InnovaMRBot.Helpers;
using InnovaMRBot.Models;
using InnovaMRBot.Services;
using TelegramBotApi.Models;
using TelegramBotApi.Telegram;

namespace InnovaMRBot.InlineCommands
{
    public abstract class BaseInlineCommand
    {
        protected const string MR_REMOVE_PATTERN = @"https?:\/\/gitlab.fortia.fr\/Fortia\/Innova\/merge_requests\/";

        protected readonly Telegram _telegramService;

        protected readonly UnitOfWork _dbContext;

        protected readonly Action<Guid, DateTime, ActionType> _scheduleAction;

        protected readonly Logger _logger;

        protected BaseInlineCommand(Telegram telegramService, UnitOfWork dbContext,
            Action<Guid, DateTime, ActionType> addAction, Logger logger)
        {
            _telegramService = telegramService;
            _dbContext = dbContext;
            _scheduleAction = addAction;
            _logger = logger;
        }

        public abstract Task WorkerAsync(Update update, string messageId);

        public virtual bool IsThisInlineCommand(string data) => false;
        
        protected string GetMrReaction(List<MessageReaction> reactions, List<Models.User> users, Models.User currentUser)
        {
            _logger.Info("Start", currentUser.UserId);
            var textForShare = new StringBuilder();

            if (reactions.Any())
            {
                var likeReaction = reactions.Where(r => r.ReactionType == ReactionType.Like).ToList();
                if (likeReaction.Any())
                {
                    textForShare.AppendLine("Members Like reaction:");
                    foreach (var messageReaction in likeReaction)
                    {
                        textForShare.AppendLine(
                            $"{users.FirstOrDefault(c => c.UserId.Equals(messageReaction.UserId)).Name} in {messageReaction.ReactionTime.GetUserTime(currentUser)}");
                    }
                }

                var badReactions = reactions.Where(r => r.ReactionType == ReactionType.DisLike).ToList();
                if (badReactions.Any())
                {
                    textForShare.AppendLine("Members Block reaction:");
                    foreach (var messageReaction in badReactions)
                    {
                        textForShare.AppendLine(
                            $"{users.FirstOrDefault(c => c.UserId.Equals(messageReaction.UserId)).Name} in {messageReaction.ReactionTime.GetUserTime(currentUser)}");
                    }
                }

                var watchReaction = reactions.Where(r => r.ReactionType == ReactionType.Watch).ToList();
                if (watchReaction.Any())
                {
                    textForShare.AppendLine("Members Watch:");
                    foreach (var messageReaction in watchReaction)
                    {
                        textForShare.AppendLine(
                            $"{users.FirstOrDefault(c => c.UserId.Equals(messageReaction.UserId)).Name} in {messageReaction.ReactionTime.GetUserTime(currentUser)}");
                    }
                }
            }
            else
            {
                textForShare.AppendLine("No reactions to this MR 😔");
            }

            _logger.Info("End", currentUser.UserId);
            return textForShare.ToString();
        }

        protected Models.User SaveIfNeedUser(TelegramBotApi.Models.User user)
        {
            var users = _dbContext.Users.GetAll();
            var needUser = users.FirstOrDefault(u => u.UserId.Equals(user.Id.ToString()));
            if (needUser == null)
            {
                var savedUser = new Models.User()
                {
                    Name = user.GetUserFullName(),
                    UserId = user.Id.ToString(),
                };

                AddOrUpdateUser(savedUser);

                needUser = savedUser;
            }

            return needUser;
        }

        private void AddOrUpdateUser(Models.User user, bool isNeedUpdate = true)
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
    }
}
