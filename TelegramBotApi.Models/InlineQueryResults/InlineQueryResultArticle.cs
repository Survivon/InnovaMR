namespace TelegramBotApi.Models.InlineQueryResults
{
    using System.Runtime.Serialization;

    [DataContract]
    public class InlineQueryResultArticle : InlineQueryResult
    {
        public InlineQueryResultArticle()
        {
            base.Type = "article";
        }

        [DataMember(Name = "title")]
        public string Title { get; set; }

        [DataMember(Name = "url")]
        public string Url { get; set; }

        [DataMember(Name = "hide_url")]
        public bool IsHideUrl { get; set; }

        [DataMember(Name = "description")]
        public string Description { get; set; }

        [DataMember(Name = "thumb_url")]
        public string ThumbUrl { get; set; }

        [DataMember(Name = "thumb_width")]
        public int? ThumbWidth { get; set; }

        [DataMember(Name = "thumb_height")]
        public int? ThumbHeight { get; set; }
    }
}
