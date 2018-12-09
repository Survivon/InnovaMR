namespace TelegramBotApi.Models.InlineQueryResults
{
    using System.Runtime.Serialization;

    [DataContract]
    public class InlineQueryResultVenue : InlineQueryResultLocation
    {
        public InlineQueryResultVenue()
        {
            base.Type = "venue";
        }
        
        [DataMember(Name = "address")]
        public string Address { get; set; }

        [DataMember(Name = "foursquare_id")]
        public string FoursquareId { get; set; }
    }
}
