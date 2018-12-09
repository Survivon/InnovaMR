namespace TelegramBotApi.Models.InlineQueryResults
{
    using System.Runtime.Serialization;
    using Enum;

    [DataContract]
    public class InlineQueryResultDocument : InlineQueryBase
    {
        public InlineQueryResultDocument()
        {
            base.Type = "document";
        }

        [DataMember(Name = "document_url")]
        public string DocumentUrl { get; set; }

        [DataMember(Name = "mime_type")]
        public string MimeType
        {
            get
            {
                switch (this.DocumentMimeType)
                {
                    case DocumentMimeType.PDF:
                        return "application/pdf";
                    case DocumentMimeType.ZIP:
                    default:
                        return "application/zip";
                }
            }
        }

        [IgnoreDataMember]
        public DocumentMimeType DocumentMimeType { get; set; }

        [DataMember(Name = "thumb_url")]
        public string ThumbUrl { get; set; }

        [DataMember(Name = "thumb_width")]
        public int? ThumbWidth { get; set; }

        [DataMember(Name = "thumb_height")]
        public int? ThumbHeight { get; set; }
    }
}
