namespace TelegramBotApi.Models.Attachment
{
    using System.Runtime.Serialization;

    [DataContract]
    public class Voice : BaseAttachment
    {
        [DataMember(Name = "file_size")]
        public int FileSize { get; set; }

        [DataMember(Name = "mime_type")]
        public string MimeType { get; set; }

        [DataMember(Name = "duration")]
        public int Duration { get; set; }
    }
}
