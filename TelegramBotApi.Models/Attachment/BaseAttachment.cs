namespace TelegramBotApi.Models.Attachment
{
    using System.Runtime.Serialization;

    [DataContract]
    public class BaseAttachment
    {
        [DataMember(Name = "file_id")]
        public string Id { get; set; }
    }
}
