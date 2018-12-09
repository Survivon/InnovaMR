namespace TelegramBotApi.Telegram.Request
{
    using System.Runtime.Serialization;

    [DataContract]
    public class SendAudioRequest : BaseDocumentRequest
    {
        /// <summary>
        /// You can either pass a file_id as String to resend a audio that is already on the Telegram servers
        /// </summary>
        [DataMember(Name = "audio")]
        public string Audio { get; set; } = null;

        /// <summary>
        /// Duration of the audio in seconds
        /// </summary>
        [DataMember(Name = "duration")]
        public int Duration { get; set; }

        [DataMember(Name = "performer")]
        public string Performer { get; set; }

        [DataMember(Name = "title")]
        public string Title { get; set; }
    }
}
