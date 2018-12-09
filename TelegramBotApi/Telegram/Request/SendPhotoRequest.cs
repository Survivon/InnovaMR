namespace TelegramBotApi.Telegram.Request
{
    using System.Runtime.Serialization;

    [DataContract]
    public class SendPhotoRequest : BaseDocumentRequest
    {
        /// <summary>
        /// You can either pass a file_id as String to resend a photo that is already on the Telegram servers
        /// </summary>
        [DataMember(Name = "photo")]
        public string Photo { get; set; } = null;
    }
}
