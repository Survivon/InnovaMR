using InnovaMRBot.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace InnovaMRBot.Bots
{
    public class ChatWorkerBot : IBot
    {
        private readonly ILogger _logger;
        private readonly ChatStateService _service;

        public ChatWorkerBot(ChatStateService service, ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _logger = loggerFactory.CreateLogger<EchoWithCounterBot>();
            _logger.LogTrace("EchoBot turn start.");
            _service = service ?? throw new System.ArgumentNullException(nameof(service));
        }

        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = new CancellationToken())
        {
            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                var activity = turnContext.Activity;

                var incomingMessage = activity.AsMessageActivity();
                var mentionFound = (incomingMessage.Entities?.Where(entity => String.Compare(entity.Type, "mention", ignoreCase: true) == 0)
                                        .Select(e => e.Properties.ToObject<Mention>())
                                        .ToArray() ?? new Mention[0])
                    .Where(mention => mention.Mentioned.Id == activity.Recipient.Id)
                    .FirstOrDefault();

                if (mentionFound != null)
                {
                    foreach (var mention in activity.GetMentions().Where(mention => mention.Mentioned.Id == activity.Recipient.Id))
                    {
                        int index = mention.Text.IndexOf('>');
                        var nextIndex = mention.Text.IndexOf('<', index);
                        var mentionText = mention.Text.Substring(index + 1, nextIndex - index - 1);
                        activity.Text = activity.Text.Replace(mentionText, "");
                    }
                }

                var setting = JsonConvert.SerializeObject(activity, Formatting.Indented,
                    settings: new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All, });

                //var connector = new ConnectorClient(new Uri(activity.ServiceUrl));

                //Activity reply = activity.CreateReply(setting);

                //await connector.Conversations.ReplyToActivityAsync(reply);

                var rr = activity.GetConversationReference();

                var repl = activity.CreateReply();

                repl.TextFormat = "plain";
                repl.Text = setting;

                await turnContext.SendActivityAsync(repl);

                //await SubscribeUser(turnContext.Adapter, rr, setting);
            }
            else
            {

                var setting = JsonConvert.SerializeObject(turnContext.Activity, Formatting.Indented,
                    settings: new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All, });


                var repl = turnContext.Activity.CreateReply();

                repl.TextFormat = "plain";
                repl.Text = setting;

                await turnContext.SendActivityAsync(repl);
            }
        }

        private async Task SubscribeUser(BotAdapter adapter, ConversationReference reference, string message)
        {
            // Send a proactive message.
            await adapter.ContinueConversationAsync(reference.Bot.Id, reference, new BotCallbackHandler(async delegate (ITurnContext context, CancellationToken token) { await context.SendActivityAsync(message); }), CancellationToken.None);
        }

        //private async void SubscribeUser(BotFrameworkAdapter adapter, string appId, ConversationReference reference)
        //{
        //    // Send a proactive message.
        //    await adapter.ContinueConversationAsync(appId, reference, new BotCallbackHandler(async delegate(ITurnContext context, CancellationToken token) { await context.SendActivityAsync("Some action"); }), CancellationToken.None);
        //}
    }
}
