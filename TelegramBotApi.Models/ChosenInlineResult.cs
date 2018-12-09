namespace TelegramBotApi.Models
{
    using System.Runtime.Serialization;
    using Attachment;

    [DataContract]
    public class ChosenInlineResult
    {
        [DataMember(Name = "result_id")]
        public string Id { get; set; }

        [DataMember(Name = "from")]
        public User Sender { get; set; }

        [DataMember(Name = "location")]
        public Location Location { get; set; }

        [DataMember(Name = "inline_message_id")]
        public string InlineMessageId { get; set; }

        [DataMember(Name = "query")]
        public string Query { get; set; }
    }
}
