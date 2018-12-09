namespace TelegramBotApi.Models.Attachment
{
    using System.Runtime.Serialization;

    [DataContract]
    public class File
    {
        [DataMember(Name = "file_id")]
        public string FileId { get; set; }

        [DataMember(Name = "file_size")]
        public int FileSize { get; set; }

        [DataMember(Name = "file_path")]
        public string FilePath { get; set; }
    }
}
