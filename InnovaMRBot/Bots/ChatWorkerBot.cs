using InnovaMRBot.Services;
using Microsoft.Bot.Builder;
using Microsoft.Extensions.Logging;
using System;
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

        }
    }
}
