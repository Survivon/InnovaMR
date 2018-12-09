namespace TelegramBotApi.Models
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using Attachment;

    [DataContract]
    public class UserProfilePhotos
    {
        [DataMember(Name = "total_count")]
        public int TotalCount { get; set; }

        [DataMember(Name = "photos")]
        public List<PhotoSize> Photos { get; set; }
    }
}
