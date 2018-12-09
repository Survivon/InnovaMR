namespace TelegramBotApi.Models.InlineQueryResults
{
    using System.Runtime.Serialization;

    [DataContract]
    public class InlineQueryResultCachedAudio : InlineQueryResult
    {
        public InlineQueryResultCachedAudio()
        {
            base.Type = "audio";
        }

        [DataMember(Name = "caption")]
        public string Caption { get; set; }

        [DataMember(Name = "audio_file_id")]
        public string AudioFileId { get; set; }
    }
}
