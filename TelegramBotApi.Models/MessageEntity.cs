namespace TelegramBotApi.Models
{
    using System.Runtime.Serialization;

    [DataContract]
    public class MessageEntity
    {
        [DataMember(Name = "type")]
        public string Type { get; set; }

        [DataMember(Name = "offset")]
        public int Offset { get; set; }

        [DataMember(Name = "length")]
        public int Length { get; set; }

        [DataMember(Name = "url")]
        public string Url { get; set; }
    }
}
