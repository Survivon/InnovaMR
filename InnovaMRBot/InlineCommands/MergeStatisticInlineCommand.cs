using InnovaMRBot.Models;
using InnovaMRBot.Models.Enum;
using InnovaMRBot.Repository;
using InnovaMRBot.Services;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TelegramBotApi.Extension;
using TelegramBotApi.Models;
using TelegramBotApi.Telegram;
using TelegramBotApi.Telegram.Request;

namespace InnovaMRBot.InlineCommands
{
    public class MergeStatisticInlineCommand : BaseInlineCommand
    {
        public MergeStatisticInlineCommand(Telegram telegramService, UnitOfWork dbContext, Action<Guid, DateTime, ActionType> addAction, Logger logger) 
            : base(telegramService, dbContext, addAction, logger)
        {

        }

        public override bool IsThisInlineCommand(string data)
        {
            return data.Equals(Glossary.InlineAction.START_WATCH);
        }

        public override async Task WorkerAsync(Update update, string messageId)
        {
            _logger.Info("MergeStatisticInlineCommand - Start", update.CallbackQuery.Sender.Id.ToString());

            if (string.IsNullOrEmpty(messageId))
            {
                messageId = update.CallbackQuery.Message.Id.ToString();
            }

            var conversations = _dbContext.Conversations.GetAll();
            var conversation = conversations.FirstOrDefault(c => c.MRChat != null);

            if (conversation == null) return;

            var users = _dbContext.Users.GetAll().ToList();
            var currentUser = SaveIfNeedUser(update.CallbackQuery.Sender);

            var needMr = conversation.ListOfMerge.FirstOrDefault(m => m.TelegramMessageId.Equals(messageId));
            if (needMr != null)
            {
                var textForShare = new StringBuilder();
                textForShare.AppendLine($"Reaction for MR {new Regex(MR_REMOVE_PATTERN).Replace(needMr.MrUrl, string.Empty)} by {users.FirstOrDefault(c => c.UserId.Equals(needMr.OwnerId)).Name}");
                textForShare.AppendLine();

                textForShare.AppendLine(GetMrReaction(needMr.Reactions, users, currentUser));

                _telegramService.SendCallbackAnswerAsync(new AnswerCallbackQueryRequest()
                {
                    IsNeedShowAlert = true,
                    Text = textForShare.ToString(),
                    CallbackId = update.CallbackQuery.Id,
                }).ConfigureAwait(false);
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

                    var textForShare = new StringBuilder();
                    textForShare.AppendLine($"Reaction for MR {new Regex(MR_REMOVE_PATTERN).Replace(versionedMr.MrUrl, string.Empty)} by {users.FirstOrDefault(c => c.UserId.Equals(versionedMr.OwnerId)).Name}");
                    textForShare.AppendLine();

                    textForShare.AppendLine(GetMrReaction(versionedMr.Reactions, users, currentUser));

                    _telegramService.SendCallbackAnswerAsync(new AnswerCallbackQueryRequest()
                    {
                        IsNeedShowAlert = true,
                        Text = textForShare.ToString(),
                        CallbackId = update.CallbackQuery.Id,
                    }).ConfigureAwait(false);
                }
            }

            _logger.Info("MergeStatisticInlineCommand - End", update.CallbackQuery.Sender.Id.ToString());
        }
    }
}
