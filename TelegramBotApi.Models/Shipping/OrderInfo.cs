using System.Runtime.Serialization;

namespace TelegramBotApi.Models.Shipping
{
    [DataContract]
    public class OrderInfo
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "phone_number")]
        public string PhoneNumber { get; set; }

        [DataMember(Name = "email")]
        public string Email { get; set; }

        [DataMember(Name = "shipping_address")]
        public ShippingAddress Address { get; set; }
    }
}
