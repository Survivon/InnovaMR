using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using InnovaMRBot.Helpers;
using InnovaMRBot.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace InnovaMRBot.Services
{
    public class ChatStateService
    {
        #region Constants

        private const string MARK_MR_CONVERSATION = "/start MR chat";

        private const string MARK_ALERT_CONVERSATION = "/start alert chat";

        private const string REMOVE_MR_CONVERSATION = "/remove MR chat";

        private const string REMOVE_ALERT_CONVERSATION = "/remove alert chat";

        private const string SETUP_ADMIN = "/setup admin";

        private const string REMOVE_ADMIN = "/remove admin";

        private const string GET_STATISTIC = "/get stat";

        private const string GET_UNMARKED_MR = "/get MR";

        private const string GET_MY_UNMARKED_MR = "/get my MR";

        private const string MR_PATTERN = @"https?:\/\/gitlab.fortia.fr\/Fortia\/Innova\/merge_requests\/[0-9]+";

        private const string TICKET_PATTERN = @"https?:\/\/fortia.atlassian.net\/browse\/\w+-[0-9]+";

        private const string CONVERSATION_KEY = "conversation";

        private const string USER_SETTING_KEY = "users";

        private List<string> _changesNotation = new List<string>()
        {
            "UPDATED",
            "ИЗМЕНЕНО"
        };

        #endregion

        public CustomConversationState ConversationState { get; }

        public ChatStateService(CustomConversationState conversationState)
        {
            ConversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
        }

        public ResponseMessage ReturnMessage(ITurnContext turnContext)
        {
            var returnMessage = new ResponseMessage();

            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                switch (turnContext.Activity.Text)
                {
                    case MARK_MR_CONVERSATION:

                        break;
                    case MARK_ALERT_CONVERSATION:

                        break;
                    case REMOVE_MR_CONVERSATION:

                        break;
                    case REMOVE_ALERT_CONVERSATION:

                        break;
                    case SETUP_ADMIN:

                        break;
                    case REMOVE_ADMIN:

                        break;
                    case GET_STATISTIC:

                        break;
                    case GET_UNMARKED_MR:

                        break;
                    case GET_MY_UNMARKED_MR:

                        break;
                    default:

                        break;
                }
            }
            else if (turnContext.Activity.Type == ActivityTypes.MessageReaction)
            {
                //
            }

            return returnMessage;
        }

        private async Task<string> GetMrMessageAsync(ITurnContext context)
        {
            var messageText = context.Activity.Text;

            var mrRegex = new Regex(MR_PATTERN);

            var mrUrlMatch = mrRegex.Match(messageText);
            var mrUrl = mrUrlMatch.Value;

            if (await IsMrContainceAsync(mrUrl))
            {
                // for updatedTicket

            }
            else
            {
                var mrMessage = new MergeSetting();

                mrMessage.MrUrl = mrUrl;

                var ticketRegex = new Regex(TICKET_PATTERN);

                var ticketMatches = ticketRegex.Matches(messageText);

                var description = messageText.Replace(mrUrl, string.Empty);

                foreach (Match ticketMatch in ticketMatches)
                {
                    mrMessage.TicketsUrl.Add(ticketMatch.Value);
                    description = description.Replace(ticketMatch.Value, string.Empty);
                }

                mrMessage.PublishDate = context.Activity.Timestamp;

                var lineOfMessage = description.Split('\r').ToList();
                var firstLine = lineOfMessage.FirstOrDefault();

                if (_changesNotation.Any(c => c.Equals(firstLine, StringComparison.InvariantCultureIgnoreCase)))
                {
                    lineOfMessage.RemoveAt(0);
                    description = string.Join('\r', lineOfMessage);
                }

                mrMessage.Description = description;
            }
        }

        #region Helpers

        private async Task<bool> IsMrContainceAsync(string mrUrl)
        {
            if (string.IsNullOrEmpty(mrUrl)) return false;

            var conversations = await GetCurrentConversationsAsync();

            if (conversations?.BotConversation == null || !conversations.BotConversation.Any()) return false;

            var mrChat = conversations.BotConversation.FirstOrDefault(c => c.IsMergeRequestConversation);

            if (mrChat?.ListOfMerge == null || !mrChat.ListOfMerge.Any()) return false;

            return mrChat.ListOfMerge.Any(c => c.MrUrl.Equals(mrUrl, StringComparison.InvariantCultureIgnoreCase));
        }

        private async Task<Conversations> GetCurrentConversationsAsync()
        {
            var storeProvider = ConversationState.Storage;

            var result = await storeProvider.ReadAsync(new[] { CONVERSATION_KEY }, CancellationToken.None);

            if (result == null || !result.Any() || !result.ContainsKey(CONVERSATION_KEY))
            {
                await GetNewConversationAsync(storeProvider);
            }

            if (result.TryGetValue(CONVERSATION_KEY, out var conversations) && conversations is Conversations conversations1)
            {
                return conversations1;
            }

            return await GetNewConversationAsync(storeProvider);
        }

        private async Task<Conversations> GetNewConversationAsync(IStorage storeProvider)
        {
            var newConversations = new Conversations();

            await storeProvider.WriteAsync(
                new Dictionary<string, object>()
            {
                { CONVERSATION_KEY, newConversations },
            }, CancellationToken.None);

            return newConversations;
        }

        #endregion

    }
}
