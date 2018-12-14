using TelegramBotApi.Models.Shipping;

namespace TelegramBotApi.Models
{
    using System.Runtime.Serialization;

    [DataContract]
    public class Update
    {
        [DataMember(Name = "update_id")]
        public int Id { get; set; }

        [DataMember(Name = "message")]
        public Message Message { get; set; }

        [DataMember(Name = "edited_message")]
        public Message EditedMessage { get; set; }

        [DataMember(Name = "channel_post")]
        public Message ChanelMessage { get; set; }

        [DataMember(Name = "edited_channel_post")]
        public Message EditedChanelMessage { get; set; }

        [DataMember(Name = "inline_query")]
        public InlineQuery InlineQuery { get; set; }

        [DataMember(Name = "chosen_inline_result")]
        public ChosenInlineResult InlineResult { get; set; }

        [DataMember(Name = "callback_query")]
        public CallbackQuery CallbackQuery { get; set; }

        [DataMember(Name = "shipping_query")]
        public ShippingQuery ShippingQuery { get; set; }

        [DataMember(Name = "pre_checkout_query")]
        public PreCheckoutQuery PreCheckoutQuery { get; set; }
    }
}
