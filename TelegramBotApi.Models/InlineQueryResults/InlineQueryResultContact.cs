namespace TelegramBotApi.Models.InlineQueryResults
{
    using System.Runtime.Serialization;

    [DataContract]
    public class InlineQueryResultContact : InlineQueryResult
    {
        public InlineQueryResultContact()
        {
            base.Type = "contact";
        }

        [DataMember(Name = "phone_number")]
        public string PhoneNumber { get; set; }

        [DataMember(Name = "first_name")]
        public string FirstName { get; set; }

        [DataMember(Name = "last_name")]
        public string LastName { get; set; }

        [DataMember(Name = "thumb_url")]
        public string ThumbUrl { get; set; }

        [DataMember(Name = "thumb_width")]
        public int? ThumbWidth { get; set; }

        [DataMember(Name = "thumb_height")]
        public int? ThumbHeight { get; set; }
    }
}
