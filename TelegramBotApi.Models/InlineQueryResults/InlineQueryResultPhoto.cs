namespace TelegramBotApi.Models.InlineQueryResults
{
    using System.Runtime.Serialization;

    [DataContract]
    public class InlineQueryResultPhoto : InlineQueryBase
    {
        public InlineQueryResultPhoto()
        {
            base.Type = "photo";
        }

        [DataMember(Name = "photo_url")]
        public string PhotoUrl { get; set; }

        [DataMember(Name = "thumb_url")]
        public string ThumbUrl { get; set; }

        [DataMember(Name = "photo_width")]
        public int? PhotoWidth { get; set; }

        [DataMember(Name = "photo_height")]
        public int? PhotoHeight { get; set; }
    }
}
