using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using InnovaMRBot.Models;
using InnovaMRBot.Repository;
using Microsoft.Extensions.DependencyInjection;
using TelegramBotApi.Extension;
using TelegramBotApi.Models.Enum;
using TelegramBotApi.Telegram;
using TelegramBotApi.Telegram.Request;

namespace InnovaMRBot.Services.Hosted
{
    public class BackgroundService : IHostedService, IDisposable
    {
        private DateTime _alertMRTime = new DateTime(2018, 12, 12, 15, 00, 00);
        private DateTime _cleanTempTime = new DateTime(2018, 12, 12, 0, 0, 0);

        private readonly ILogger _logger;
        private Timer _timer;
        private readonly UnitOfWork _dbContext;
        private readonly Telegram _telegramService;
        private object _locker = new object();

        public IServiceProvider Services { get; }

        public BackgroundService(ILogger<BackgroundService> logger, IServiceProvider services)
        {
            _logger = logger;
            Services = services;

            using (var scope = Services.CreateScope())
            {
                var scopedProcessingService =
                    scope.ServiceProvider
                        .GetRequiredService<IServiceScopeFactory>();

                var scope1 = scopedProcessingService.CreateScope();
                var provider = scope1.ServiceProvider;
                var dbContext = provider.GetRequiredService<BotContext>();

                _telegramService = provider.GetRequiredService<Telegram>();
                _dbContext = new UnitOfWork(dbContext);
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Background Service is starting.");

            var needTimeTime = DateTime.UtcNow;

            if (needTimeTime.Hour >= 15)
            {
                needTimeTime = new DateTime(needTimeTime.Year, needTimeTime.Month, needTimeTime.Day + 1, 15, 0, 0);
            }
            else
            {
                needTimeTime = new DateTime(needTimeTime.Year, needTimeTime.Month, needTimeTime.Day, 15, 0, 0);
            }

            var startTime = needTimeTime.Subtract(DateTime.UtcNow);

            _timer = new Timer(CheckConversation, null, startTime, TimeSpan.FromSeconds(3600));

            return Task.CompletedTask;
        }

        private void CheckConversation(object state)
        {
            _logger.LogInformation("Background Service is working.");

            var currentTime = DateTime.UtcNow;

            if (currentTime.Hour == _alertMRTime.Hour)
            {
                lock (_locker)
                {
                    var conv = _dbContext.Conversations.GetAll();

                    var needConversation = conv.FirstOrDefault();

                    var merges = needConversation.ListOfMerge.ToList();

                    MapUsers(merges, _dbContext.Users.GetAll().ToList());

                    var needMR = new List<Tuple<string, string, int>>();

                    foreach (var merge in merges)
                    {
                        var lastVersion = GetLastVersion(merge);

                        if (lastVersion.Reactions.Count < 2)
                        {
                            needMR.Add(new Tuple<string, string, int>(merge.Owner.Name, merge.MrUrl, 2 - lastVersion.Reactions.Count));
                        }
                    }

                    if (!needMR.Any()) return;

                    var resultBuilder = new StringBuilder();

                    resultBuilder.AppendLine("<b>UNMARKED MergeRequests</b>");

                    foreach (var tuple in needMR)
                    {
                        resultBuilder.AppendLine($"MR: {tuple.Item2} by <i>{tuple.Item1}</i>");
                    }

                    resultBuilder.Append("\n\r");

                    var mrIconPath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "MRBigIcon.png");

                    _telegramService.SendPhotoAsync(new SendPhotoRequest()
                    {
                        Caption = resultBuilder.ToString(),
                        ChatId = needConversation.MRChat.Id,
                        FormattingMessageType = FormattingMessageType.HTML,
                    }, mrIconPath).ConfigureAwait(false);
                }
            }
            else if (currentTime.Hour == _cleanTempTime.Hour)
            {
                var directoryPath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "temp");

                if (Directory.Exists(directoryPath))
                {
                    var di = new DirectoryInfo(directoryPath);

                    foreach (var file in di.GetFiles())
                    {
                        file.Delete();
                    }

                    foreach (var dir in di.GetDirectories())
                    {
                        dir.Delete(true);
                    }
                }
            }
        }

        private static void MapUsers(List<MergeSetting> merge, List<User> usersList)
        {
            if (usersList == null || !usersList.Any()) return;

            foreach (var mergeSetting in merge)
            {
                mergeSetting.Owner = usersList.FirstOrDefault(u => u.UserId.Equals(mergeSetting.OwnerId));
                foreach (var versionedMergeRequest in mergeSetting.VersionedSetting)
                {
                    foreach (var reaction in versionedMergeRequest.Reactions)
                    {
                        reaction.User = usersList.FirstOrDefault(u => u.UserId.Equals(reaction.UserId));
                    }
                }

                foreach (var mergeSettingReaction in mergeSetting.Reactions)
                {
                    mergeSettingReaction.User = usersList.FirstOrDefault(u => u.UserId.Equals(mergeSettingReaction.UserId));
                }
            }
        }

        private static VersionedMergeRequest GetLastVersion(MergeSetting merge)
        {
            var result = new VersionedMergeRequest()
            {
                PublishDate = merge.PublishDate,
                Reactions = merge.Reactions,
            };

            if (merge.VersionedSetting != null && merge.VersionedSetting.Any())
            {
                return merge.VersionedSetting.FirstOrDefault(c =>
                    c.PublishDate == merge.VersionedSetting.Max(m => m.PublishDate));
            }

            return result;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Background Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
