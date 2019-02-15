using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using InnovaMRBot.Models;
using InnovaMRBot.Repository;
using Microsoft.AspNetCore.Mvc;
using TelegramBotApi.Extension;
using TelegramBotApi.Models.Enum;
using TelegramBotApi.Models.Keyboard;
using TelegramBotApi.Telegram;
using TelegramBotApi.Telegram.Request;
using Action = InnovaMRBot.Models.Action;

namespace InnovaMRBot.Controllers
{
    [Route("")]
    public class SchedulerController : Controller
    {
        protected const string TICKET_NUMBER_PATTERN = @"\w+[0-9]+";

        private readonly UnitOfWork _dbContext;
        private readonly Telegram _telegram;

        public SchedulerController(UnitOfWork dbContext, Telegram telegram)
        {
            _dbContext = dbContext;
            _telegram = telegram;
        }

        [HttpPost]
        [Route("action")]
        public void GetAction([FromBody]string id)
        {
            var action = _dbContext.Actions.Get(Guid.Parse(id));

            var needTimeToStart = DateTime.UtcNow.Subtract(action.ExecDate);

            var data = new object[] { _dbContext, _telegram, id };

            var timer = new Timer(Callback, data, needTimeToStart, TimeSpan.FromMilliseconds(-1));
        }

        private void Callback(object state)
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
                                CallbackData = $"/success_reaction_{Regex.Match(merge.MrUrl, TICKET_NUMBER_PATTERN).Value}",
                            },
                            new InlineKeyboardButton()
                            {
                                Text = "MR",
                                Url = merge.MrUrl,
                            },
                            new InlineKeyboardButton()
                            {
                                Text = "🚫",
                                CallbackData = $"/bad_reaction_{Regex.Match(merge.MrUrl, TICKET_NUMBER_PATTERN).Value}",
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
                                    CallbackData = $"/success_reaction_{Regex.Match(merge.MrUrl, TICKET_NUMBER_PATTERN).Value}",
                                },
                                new InlineKeyboardButton()
                                {
                                    Text = "MR",
                                    Url = merge.MrUrl,
                                },
                                new InlineKeyboardButton()
                                {
                                    Text = "🚫",
                                    CallbackData = $"/bad_reaction_{Regex.Match(merge.MrUrl, TICKET_NUMBER_PATTERN).Value}",
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

        private static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }
    }
}
