namespace TelegramBotApi.Telegram.Request
{
    using System.Runtime.Serialization;

    [DataContract]
    public class EditMessageTextRequest : SendMessageRequest
    {
        [DataMember(Name = "inline_message_id")]
        public string InlineMessageId { get; set; }

        [DataMember(Name = "message_id")]
        public string EditMessageId { get; set; }
    }
}
