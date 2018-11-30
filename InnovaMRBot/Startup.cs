using InnovaMRBot.Bots;
using InnovaMRBot.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using InnovaMRBot.Helpers;

namespace InnovaMRBot
{
    public class Startup
    {
        private ILoggerFactory _loggerFactory;
        private bool _isProduction = false;

        public Startup(IHostingEnvironment env)
        {
            _isProduction = env.IsProduction();
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddBot<ChatWorkerBot>(options =>
            {
                var secretKey = Configuration.GetSection("botFileSecret")?.Value;
                var botFilePath = Configuration.GetSection("botFilePath")?.Value;

                var environment = _isProduction ? "production" : "development";

                var botConfig = BotConfiguration.Load(string.IsNullOrEmpty(botFilePath) ? $@".\BotConfiguration{environment}.bot" : string.Format(botFilePath, environment), secretKey);
                services.AddSingleton(sp => botConfig ?? throw new InvalidOperationException($"The .bot config file could not be loaded. ({botConfig})"));

                var service = botConfig.Services.Where(s => s.Type == "endpoint" && s.Name == environment).FirstOrDefault();
                if (!(service is EndpointService endpointService))
                {
                    throw new InvalidOperationException($"The .bot file does not contain an endpoint with name '{environment}'.");
                }

                options.CredentialProvider = new SimpleCredentialProvider(endpointService.AppId, endpointService.AppPassword);

                ILogger logger = _loggerFactory.CreateLogger<ChatWorkerBot>();

                options.OnTurnError = async (context, exception) =>
                {
                    logger.LogError($"Exception caught : {exception}");
                    var ss = exception.Data;
                    var dd = exception.Source;
                    await context.SendActivityAsync($"{exception.Message} \n \n {exception.StackTrace}");
                };

                IStorage dataStore = new MemoryStorage();

                var conversationState = new CustomConversationState(dataStore);

                options.State.Add(conversationState);
            });

            services.AddSingleton<ChatStateService>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<BotFrameworkOptions>>().Value;
                if (options == null)
                {
                    throw new InvalidOperationException("BotFrameworkOptions must be configured prior to setting up the state accessors");
                }

                var conversationState = options.State.OfType<CustomConversationState>().FirstOrDefault();
                if (conversationState == null)
                {
                    throw new InvalidOperationException("ConversationState must be defined and added before adding conversation-scoped state accessors.");
                }

                var accessors = new ChatStateService(conversationState);

                return accessors;
            });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;

            app.UseDefaultFiles()
                .UseStaticFiles()
                .UseBotFramework();
        }
    }
}
