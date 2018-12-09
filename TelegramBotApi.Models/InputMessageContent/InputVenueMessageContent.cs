namespace TelegramBotApi.Models.InputMessageContent
{
    using System.Runtime.Serialization;

    [DataContract]
    public class InputVenueMessageContent : InputLocationMessageContent
    {
        [DataMember(Name = "title")]
        public string Title { get; set; }

        [DataMember(Name = "address")]
        public string Address { get; set; }

        [DataMember(Name = "foursquare_id")]
        public string FoursquareId { get; set; }
    }
}
