
using System.Collections.Generic;
using TelegramBotApi.Models;

namespace TelegramBotApi.Telegram
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Events;
    using Extension;

    public class Telegram
    {
        private const string UrlPattern = "https://api.telegram.org/bot{0}/{1}";

        private string _botTokenKey = "744048631:AAE_4sUEI5WcDxAZ-HpHWCv-vmFbLI00FNQ";
        private string _webhookUrl = "";

        public Telegram(bool isStartLongPool = true)
        {
            if (isStartLongPool)
            {
                this.StartLongPull();
            }
        }

        public Telegram(string botToken) : this()
        {
            this._botTokenKey = botToken;
        }

        public Telegram(string webhookUrl, string botToken = null) : this(string.IsNullOrEmpty(webhookUrl))
        {
            this._webhookUrl = webhookUrl;
            if (!string.IsNullOrEmpty(botToken))
            {
                _botTokenKey = botToken;
            }

            if (!string.IsNullOrEmpty(webhookUrl))
            {
                this.SetWebhookAsync(webhookUrl).ConfigureAwait(false);
            }
        }

        public void SetupChanges(List<Update> updates)
        {
            if (updates == null || !updates.Any()) return;

            this.OnUpdateReceive?.Invoke(this, new UpdateEventArgs() { Updates = updates });
        }

        public event EventHandler<UpdateEventArgs> OnUpdateReceive;

        public void StartLongPull()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    var updates = await this.GetUpdatesAsync();
                    if (updates != null && updates.Any())
                    {
                        this.OnUpdateReceive?.Invoke(this, new UpdateEventArgs() { Updates = updates });
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(2000));
                }
            }).ConfigureAwait(false);
        }

        public string GetFullPathUrl(string method) => string.Format(UrlPattern, _botTokenKey, method);
    }
}
