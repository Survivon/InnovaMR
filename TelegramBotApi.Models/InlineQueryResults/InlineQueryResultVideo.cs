namespace TelegramBotApi.Models.InlineQueryResults
{
    using System;
    using System.Runtime.Serialization;
    using Enum;

    [DataContract]
    public class InlineQueryResultVideo : InlineQueryBase
    {
        public InlineQueryResultVideo()
        {
            base.Type = "video";
        }

        [DataMember(Name = "video_url")]
        public string VideoUrl { get; set; }

        /// <summary>
        /// Can by only "text/html" or "video/mp4" type
        /// </summary>
        [DataMember(Name = "mime_type")]
        public string MimeType
        {
            get
            {
                switch (this.VideoMimeType)
                {
                    case VideoMimeType.TextHtml:
                        return "text/html";
                    case VideoMimeType.VideoMp4:
                    default:
                        return "video/mp4";
                }
            }
        }

        [IgnoreDataMember]
        public VideoMimeType VideoMimeType { get; set; }

        [DataMember(Name = "thumb_url")]
        public string ThumbUrl { get; set; }

        [DataMember(Name = "video_width")]
        public int? VideoWidth { get; set; }

        [DataMember(Name = "video_height")]
        public int? VideoHeight { get; set; }

        [DataMember(Name = "video_duration")]
        public int? VideoDuration { get; set; }
    }
}
