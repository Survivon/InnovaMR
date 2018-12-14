using System.Runtime.Serialization;

namespace TelegramBotApi.Models.Shipping
{
    [DataContract]
    public class PreCheckoutQuery
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "from")]
        public User From { get; set; }

        [DataMember(Name = "currency")]
        public string Currency { get; set; }

        [DataMember(Name = "total_amount")]
        public int TotalAmount { get; set; }

        [DataMember(Name = "invoice_payload")]
        public string InvoicePayload { get; set; }

        [DataMember(Name = "shipping_option_id")]
        public string ShippingOptionId { get; set; }

        [DataMember(Name = "order_info")]
        public OrderInfo OrderInfo { get; set; }
    }
}
