﻿namespace TelegramBotApi.Models.Attachment
{
    using System.Runtime.Serialization;

    [DataContract]
    public class Venue
    {
        [DataMember(Name = "location")]
        public Location Location { get; set; }

        [DataMember(Name = "title")]
        public string Title { get; set; }

        [DataMember(Name = "address")]
        public string Address { get; set; }

        [DataMember(Name = "foursquare_id")]
        public string FoursquareId { get; set; }
    }
}
