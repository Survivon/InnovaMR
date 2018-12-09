namespace TelegramBotApi.Models.Attachment
{
    using System.Runtime.Serialization;

    [DataContract]
    public class Document : BaseAttachment
    {
        [DataMember(Name = "thumb")]
        public PhotoSize Thumb { get; set; }

        [DataMember(Name = "file_name")]
        public string FileName { get; set; }

        [DataMember(Name = "mime_type")]
        public string MimeType { get; set; }

        [DataMember(Name = "file_size")]
        public int FileSize { get; set; }
    }
}
