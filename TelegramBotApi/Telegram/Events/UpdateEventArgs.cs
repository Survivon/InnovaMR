
namespace TelegramBotApi.Telegram.Events
{
    using System;
    using System.Collections.Generic;
    using Models;

    public class UpdateEventArgs : EventArgs
    {
        public List<Update> Updates { get; set; }
    }
}
