using InnovaMRBot.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
                var mentionFound = (incomingMessage.Entities?.Where(entity => string.Compare(entity.Type, "mention", StringComparison.OrdinalIgnoreCase) == 0)
                                        .Select(e => e.Properties.ToObject<Mention>())
                                        .ToArray() ?? new Mention[0])
                    .FirstOrDefault(mention => mention.Mentioned.Id == activity.Recipient.Id);

                if (mentionFound != null)
                {
                    foreach (var mention in activity.GetMentions().Where(mention => mention.Mentioned.Id == activity.Recipient.Id))
                    {
                        var index = mention.Text.IndexOf('>');
                        var nextIndex = mention.Text.IndexOf('<', index);
                        var mentionText = mention.Text.Substring(index + 1, nextIndex - index - 1);
                        activity.Text = activity.Text.Replace(mentionText, string.Empty);
                    }
                }

                var setting = JsonConvert.SerializeObject(activity, Formatting.Indented,
                    new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All, });
                
                var repl = activity.CreateReply();

                repl.TextFormat = "plain";
                repl.Text = setting;

                if (activity.Text.Equals("/get all message"))
                {
                    var act = await _service.ReturnMessage(turnContext);
                    await turnContext.SendActivityAsync(act).ConfigureAwait(false);
                    return;
                }

                await turnContext.SendActivityAsync(repl);
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
    }
}
