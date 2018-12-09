namespace TelegramBotApi.Models.InlineQueryResults
{
    using System.Runtime.Serialization;

    [DataContract]
    public class InlineQueryResultLocation : InlineQueryResult
    {
        public InlineQueryResultLocation()
        {
            base.Type = "location";
        }
        
        [DataMember(Name = "latitude")]
        public float Latitude { get; set; }

        [DataMember(Name = "longitude")]
        public float Longitude { get; set; }

        [DataMember(Name = "title")]
        public string Title { get; set; }
        
        [DataMember(Name = "thumb_url")]
        public string ThumbUrl { get; set; }

        [DataMember(Name = "thumb_width")]
        public int? ThumbWidth { get; set; }

        [DataMember(Name = "thumb_height")]
        public int? ThumbHeight { get; set; }
    }
}
