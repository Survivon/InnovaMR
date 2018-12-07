using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace InnovaMRBot.Services.Hosted
{
    public class BackgroundService : IHostedService, IDisposable
    {
        private DateTime _alertMRTime = new DateTime(2018, 12, 12, 15, 00, 00);

        private readonly ILogger _logger;
        private Timer _timer;

        public BackgroundService(ILogger<BackgroundService> logger)
        {
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Background Service is starting.");

            _timer = new Timer(CheckConversation, null, TimeSpan.Zero, TimeSpan.FromSeconds(15));

            return Task.CompletedTask;
        }

        private void CheckConversation(object state)
        {
            _logger.LogInformation("Background Service is working.");

            var currentTime = DateTime.UtcNow;
            if (currentTime.Hour == _alertMRTime.Hour)
            {
                // publish MR

            }
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
