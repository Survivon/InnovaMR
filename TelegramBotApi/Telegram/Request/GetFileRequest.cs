namespace TelegramBotApi.Telegram.Request
{
    using System.Runtime.Serialization;

    [DataContract]
    public class GetFileRequest
    {
        [DataMember(Name = "file_id")]
        public int FileId { get; set; }
    }
}
