using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TelegramBotApi.Models
{
    [DataContract]
    public class WebhookEntity
    {
        public string Url { get; set; }

        public int? MaxConnections { get; set; }

        public List<string> AllowedUpdates { get; set; }
    }
}
