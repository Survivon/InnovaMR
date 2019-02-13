using System.Runtime.Serialization;

namespace TelegramBotApi.Telegram.Request
{
    [DataContract]
    public class RemoveMessageRequest
    {
        [DataMember(Name = "chat_id")]
        public string ChatId { get; set; }

        [DataMember(Name = "message_id")]
        public string MessageId { get; set; }
    }
}
