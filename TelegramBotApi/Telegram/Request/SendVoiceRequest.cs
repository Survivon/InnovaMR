
namespace TelegramBotApi.Telegram.Request
{
    using System.Runtime.Serialization;

    [DataContract]
    public class SendVoiceRequest : BaseDocumentRequest
    {
        /// <summary>
        /// You can either pass a file_id as String to resend a audio that is already on the Telegram servers
        /// </summary>
        [DataMember(Name = "voice")]
        public string Voice { get; set; } = null;

        /// <summary>
        /// Duration of the audio in seconds
        /// </summary>
        [DataMember(Name = "duration")]
        public int Duration { get; set; }
    }
}
