namespace TelegramBotApi.Models
{
    using System.Runtime.Serialization;

    [DataContract]
    public class CallbackQuery
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "from")]
        public User Sender { get; set; }

        [DataMember(Name = "message")]
        public Message Message { get; set; }

        [DataMember(Name = "inline_message_id")]
        public string InlineMessageId { get; set; }

        [DataMember(Name = "data")]
        public string Data { get; set; }
    }
}
