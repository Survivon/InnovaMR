using System.Runtime.Serialization;

namespace TelegramBotApi.Models.Shipping
{
    [DataContract]
    public class ShippingAddress
    {
        [DataMember(Name = "country_code")]
        public string CountryCode { get; set; }

        [DataMember(Name = "state")]
        public string State { get; set; }

        [DataMember(Name = "city")]
        public string City { get; set; }

        [DataMember(Name = "street_line1")]
        public string StreetLine1 { get; set; }

        [DataMember(Name = "street_line2")]
        public string StreetLine2 { get; set; }

        [DataMember(Name = "post_code")]
        public string PostCode { get; set; }
    }
}
