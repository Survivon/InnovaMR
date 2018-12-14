using System.Runtime.Serialization;

namespace TelegramBotApi.Models.Shipping
{
    [DataContract]
    public class ShippingQuery
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }
        
        [DataMember(Name = "from")]
        public User From { get; set; }

        [DataMember(Name = "invoice_payload")]
        public string InvoicePayload { get; set; }

        [DataMember(Name = "shipping_address")]
        public ShippingAddress ShippingAddress { get; set; }
    }
}
