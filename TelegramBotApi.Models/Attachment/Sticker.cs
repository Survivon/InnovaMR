namespace TelegramBotApi.Models.Attachment
{
    using System.Runtime.Serialization;

    [DataContract]
    public class Sticker : BaseAttachment
    {
        [DataMember(Name = "thumb")]
        public PhotoSize Thumb { get; set; }

        [DataMember(Name = "width")]
        public int Width { get; set; }

        [DataMember(Name = "height")]
        public int Height { get; set; }

        [DataMember(Name = "file_size")]
        public int FileSize { get; set; }
    }
}
