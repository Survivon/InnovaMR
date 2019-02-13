
using System.Threading.Tasks;
using InnovaMRBot.Repository;
using TelegramBotApi.Extension;
using TelegramBotApi.Models;
using TelegramBotApi.Models.Enum;
using TelegramBotApi.Telegram;
using TelegramBotApi.Telegram.Request;

namespace InnovaMRBot.Commands
{
    public class HelpCommand : BaseCommand
    {
        private const string COMMAND = "/help";

        public HelpCommand(Telegram telegram, UnitOfWork dbContext)
            : base(telegram, dbContext)
        {
            CommandId = "helpcommand";
        }

        protected override string GetCommandString()
        {
            return COMMAND;
        }

        public override async Task WorkerAsync(Update update)
        {
            _telegram.SendMessageAsync(new SendMessageRequest
            {
                Text = @"<b>How to send MR?</b>
1.Write you message with <i>MR Link</i>, <i>Ticket Link</i> and <i>Description</i>
2.If everything is correct Bot send it to chanel with other MRs
<b>How to get statistics from MRs?</b>
<i>/get stat getalldata</i> command for get all data about MR(links, publish date, reviewers, etc.)
<i>/get stat getmrreaction</i> command for get reaction on ticket
<i>/get stat getusermrreaction</i> command for get user reaction on tickets
<i>/get stat getunmarked</i> command for get count of unmarked MR per days
For all of this statistics you can add start and end date of publish date(For ex. <b>/get stat getalldata 24/11/2018 28/11/2018</b>)
🚫 - mark MR that it has some conflicts or bad code, after mark please send message to MRs owner",
                ChatId = update.Message.Chat.Id.ToString(),
                FormattingMessageType = FormattingMessageType.HTML,
            }).ConfigureAwait(false);
        }
    }
}
