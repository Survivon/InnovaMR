namespace TelegramBotApi.Models.Attachment
{
    using System.Runtime.Serialization;

    [DataContract]
    public class BaseRecorder : BaseAttachment
    {
        [DataMember(Name = "duration")]
        public int Duration { get; set; }

        [DataMember(Name = "mime_type")]
        public string MimeType { get; set; }

        [DataMember(Name = "fileSize")]
        public int FileSize { get; set; }


    }
}
