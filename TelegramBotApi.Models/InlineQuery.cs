namespace TelegramBotApi.Models
{
    using System.Runtime.Serialization;
    using Attachment;

    [DataContract]
    public class InlineQuery
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "from")]
        public User Sender { get; set; }

        [DataMember(Name = "location")]
        public Location Location { get; set; }

        [DataMember(Name = "query")]
        public string Query { get; set; }

        [DataMember(Name = "offset")]
        public string Offset { get; set; }
    }
}
