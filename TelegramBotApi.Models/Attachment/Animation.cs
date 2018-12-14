using System.Runtime.Serialization;

namespace TelegramBotApi.Models.Attachment
{
    [DataContract]
    public class Animation : PhotoSize
    {
        [DataMember(Name = "duration")]
        public int Duration { get; set; }

        [DataMember(Name = "thumb")]
        public PhotoSize Thumb { get; set; }

        [DataMember(Name = "file_name")]
        public string FileName { get; set; }

        [DataMember(Name = "mime_type")]
        public string MimeType { get; set; }
    }
}
