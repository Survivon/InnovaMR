namespace TelegramBotApi.Models.InlineQueryResults
{
    using System.Runtime.Serialization;

    [DataContract]
    public class InlineQueryResultGif : InlineQueryBase
    {
        public InlineQueryResultGif()
        {
            base.Type = "gif";
        }

        [DataMember(Name = "gif_url")]
        public string GifUrl { get; set; }

        [DataMember(Name = "thumb_url")]
        public string ThumbUrl { get; set; }

        [DataMember(Name = "gif_width")]
        public int? GifWidth { get; set; }

        [DataMember(Name = "gif_height")]
        public int? GifHeight { get; set; }
    }
}
