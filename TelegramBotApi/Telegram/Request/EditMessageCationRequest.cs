namespace TelegramBotApi.Telegram.Request
{
    using System.Runtime.Serialization;

    [DataContract]
    public class EditMessageCationRequest : SendPhotoRequest
    {
        [DataMember(Name = "inline_message_id")]
        public string InlineMessageId { get; set; }

        [DataMember(Name = "message_id")]
        public string EditMessageId { get; set; }

        [IgnoreDataMember]
        public new string Photo { get; private set; }
    }
}
