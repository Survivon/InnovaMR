
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TelegramBotApi.Models
{
    [DataContract]
    public class WebhookInfoEntity
    {
        [DataMember(Name = "url")]
        public string Url { get; set; }

        [DataMember(Name = "has_custom_certificate")]
        public bool HasCustomCertificat { get; set; }

        [DataMember(Name = "pending_update_count")]
        public int PendingUpdateEntity { get; set; }

        [DataMember(Name = "last_error_date")]
        public int LastErrorDate { get; set; }

        [DataMember(Name = "last_error_message")]
        public string LastErrorMesssage { get; set; }

        [DataMember(Name = "max_connections")]
        public int MaxConnection { get; set; }

        [DataMember(Name = "allowed_updates")]
        public List<string> AllowedUpdates { get; set; }
    }
}
