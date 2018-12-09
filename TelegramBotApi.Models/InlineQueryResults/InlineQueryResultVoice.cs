namespace TelegramBotApi.Models.InlineQueryResults
{
    using System.Runtime.Serialization;

    [DataContract]
    public class InlineQueryResultVoice : InlineQueryBase
    {
        public InlineQueryResultVoice()
        {
            base.Type = "voice";
        }

        [DataMember(Name = "voice_url")]
        public string VoiceUrl { get; set; }

        [DataMember(Name = "voice_duration")]
        public int? VoiceDuration { get; set; }
    }
}
