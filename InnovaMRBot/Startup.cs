using InnovaMRBot.Bots;
using InnovaMRBot.Helpers;
using InnovaMRBot.Services;
using InnovaMRBot.Services.Hosted;
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
using TelegramBotApi.Extension;
using TelegramBotApi.Telegram;

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

                var botConfig = MrConfigurationManager.Load(string.IsNullOrEmpty(botFilePath) ? $@".\BotConfiguration{environment}.bot" : string.Format(botFilePath, environment), secretKey);
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
#if DEBUG
                    await context.SendActivityAsync($"{exception.Message} \n \n {exception.StackTrace}");
#else
                        await context.SendActivityAsync($"Something went wrong :(");
#endif
                };

                IStorage dataStore = new MemoryStorage();

                var conversationState = new CustomConversationState(dataStore);
                
                options.State.Add(conversationState);
            });

            services.AddSingleton(sp =>
            {
            //    var secretKey = Configuration.GetSection("botFileSecret")?.Value;
            //    var botFilePath = Configuration.GetSection("botFilePath")?.Value;

            //    var logger = _loggerFactory.CreateLogger<Telegram>();

            //    var environment = _isProduction ? "production" : "development";

            //    var botConfig = MrConfigurationManager.Load(string.IsNullOrEmpty(botFilePath) ? $@".\BotConfiguration{environment}.bot" : string.Format(botFilePath, environment), secretKey);

            //    if (botConfig.TelegramSetting == null || string.IsNullOrEmpty(botConfig.TelegramSetting.WebhookUrl) ||
            //        string.IsNullOrEmpty(botConfig.TelegramSetting.BotKey)) return new Telegram();

                //var telegram = new Telegram($"{botConfig.TelegramSetting.WebhookUrl}/some", null);

                var telegram = new Telegram();

                //logger.LogInformation("Get webhook info");
                //var webhookInfo = telegram.GetWebhookInfoAsync().Result;
                //logger.LogInformation($"Webhook url {webhookInfo.Url}");
                //if (string.IsNullOrEmpty(webhookInfo.Url))
                //{
                //    logger.LogInformation($"Setup webhook {botConfig.TelegramSetting.WebhookUrl}/{botConfig.TelegramSetting.BotKey}");
                //    telegram.SetWebhookAsync($"{botConfig.TelegramSetting.WebhookUrl}/some").ConfigureAwait(false);
                //}

                return telegram;
            });

            services.AddSingleton(sp =>
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

                var telegram = sp.GetService<Telegram>();

                var accessors = new ChatStateService(conversationState, telegram);

                return accessors;
            });

            services.AddHostedService<BackgroundService>();

            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;

            var secretKey = Configuration.GetSection("botFileSecret")?.Value;
            var botFilePath = Configuration.GetSection("botFilePath")?.Value;

            var logger = _loggerFactory.CreateLogger<Telegram>();

            var environment = _isProduction ? "production" : "development";

            var botConfig = MrConfigurationManager.Load(string.IsNullOrEmpty(botFilePath) ? $@".\BotConfiguration{environment}.bot" : string.Format(botFilePath, environment), secretKey);
            
            app.UseDefaultFiles()
                .UseStaticFiles()
                .UseBotFramework().UseMvc(routes =>
                {
                    routes.MapRoute(
                        name: "default",
                        template: "{controller=Home}/{action=Index}/{id?}");

                    routes.MapRoute(
                        name: "telegramRout",
                        template: $"{botConfig?.TelegramSetting?.BotKey ?? string.Empty}",
                        defaults: new { controller = "Telegram", action = "GetUpdateFromTelegram" });
                });
            
            if (botConfig.TelegramSetting == null || string.IsNullOrEmpty(botConfig.TelegramSetting.WebhookUrl) ||
                string.IsNullOrEmpty(botConfig.TelegramSetting.BotKey)) return;

            var telegram = new Telegram($"{botConfig.TelegramSetting.WebhookUrl}/some", null);

            logger.LogInformation("Get webhook info");
            var webhookInfo = telegram.GetWebhookInfoAsync().Result;
            logger.LogInformation($"Webhook url {webhookInfo.Url}");

            if (string.IsNullOrEmpty(webhookInfo.Url))
            {
                logger.LogInformation($"Setup webhook {botConfig.TelegramSetting.WebhookUrl}/{botConfig.TelegramSetting.BotKey}");
                telegram.SetWebhookAsync($"{botConfig.TelegramSetting.WebhookUrl}/some").ConfigureAwait(false);
            }
        }
    }
}
