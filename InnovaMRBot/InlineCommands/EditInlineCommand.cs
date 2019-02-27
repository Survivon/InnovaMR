using InnovaMRBot.Commands;
using InnovaMRBot.Models.Enum;
using InnovaMRBot.Repository;
using InnovaMRBot.Services;
using System;
using System.Threading.Tasks;
using TelegramBotApi.Models;
using TelegramBotApi.Telegram;

namespace InnovaMRBot.InlineCommands
{
    public class EditInlineCommand : BaseInlineCommand
    {
        public EditInlineCommand(Telegram telegramService, UnitOfWork dbContext, Action<Guid, DateTime, ActionType> addAction, Logger logger) : base(telegramService, dbContext, addAction, logger)
        {
        }

        public override bool IsThisInlineCommand(string data)
        {
            return data.StartsWith(EditCommand.COMMAND);
        }

        public override async Task WorkerAsync(Update update, string messageId)
        {
            _logger.Info("EditInlineCommand - Start", update.CallbackQuery.Sender.Id.ToString());

            var editCommand = new EditCommand(_telegramService, _dbContext, _logger);

            update.Message = new Message
            {
                Chat = update.CallbackQuery.Message.Chat,
                Text = update.CallbackQuery.Data,
                Sender = update.CallbackQuery.Sender,
            };
            
            editCommand.WorkerAsync(update).ConfigureAwait(false);

            _logger.Info("EditInlineCommand - End", update.CallbackQuery.Sender.Id.ToString());
        }
    }
}
