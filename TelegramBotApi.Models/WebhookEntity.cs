using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TelegramBotApi.Models
{
    [DataContract]
    public class WebhookEntity
    {
        [DataMember(Name = "url")]
        public string Url { get; set; }

        [DataMember(Name = "max_connections")]
        public int? MaxConnections { get; set; }

        [DataMember(Name = "allowed_updates")]
        public List<string> AllowedUpdates { get; set; }
    }
}
