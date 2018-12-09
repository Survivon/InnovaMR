
namespace TelegramBotApi.Telegram.Request
{
    using System.Runtime.Serialization;

    [DataContract]
    public class SendDocumentRequest : BaseDocumentRequest
    {
        /// <summary>
        /// You can either pass a file_id as String to resend a photo that is already on the Telegram servers
        /// </summary>
        [DataMember(Name = "document")]
        public string Document { get; set; } = null;
    }
}
