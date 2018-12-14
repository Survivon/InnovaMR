
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TelegramBotApi.Extension;
using TelegramBotApi.Models;
using TelegramBotApi.Telegram.Events;

namespace TelegramBotApi.Telegram
{

    public class Telegram
    {
        private const string UrlPattern = "https://api.telegram.org/bot{0}/{1}";

        private string _botTokenKey = "744048631:AAE_4sUEI5WcDxAZ-HpHWCv-vmFbLI00FNQ";
        private string _webhookUrl = "";

        public Telegram()
        {

        }

        public Telegram(string botToken) : this()
        {
            this._botTokenKey = botToken;
        }

        public Telegram(string webhookUrl, string botToken = null)
        {
            if (!string.IsNullOrEmpty(botToken))
            {
                _botTokenKey = botToken;
            }

            if (!string.IsNullOrEmpty(webhookUrl))
            {
                _webhookUrl = webhookUrl;
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
