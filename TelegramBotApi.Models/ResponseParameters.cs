namespace TelegramBotApi.Models
{
    using System.Runtime.Serialization;

    [DataContract]
    public class ResponseParameters
    {
        [DataMember(Name = "migrate_to_chat_id")]
        public int MigrateToChatId { get; set; }

        [DataMember(Name = "retry_after")]
        public int RetryAfter { get; set; }
    }
}
