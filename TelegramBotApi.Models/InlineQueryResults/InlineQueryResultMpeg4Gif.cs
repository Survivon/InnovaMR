namespace TelegramBotApi.Models.InlineQueryResults
{
    using System.Runtime.Serialization;

    [DataContract]
    public class InlineQueryResultMpeg4Gif : InlineQueryBase
    {
        public InlineQueryResultMpeg4Gif()
        {
            base.Type = "mpeg4_gif";
        }

        [DataMember(Name = "mpeg4_url")]
        public string GifUrl { get; set; }

        [DataMember(Name = "thumb_url")]
        public string ThumbUrl { get; set; }

        [DataMember(Name = "mpeg4_width")]
        public int? GifWidth { get; set; }

        [DataMember(Name = "mpeg4_height")]
        public int? GifHeight { get; set; }
    }
}
