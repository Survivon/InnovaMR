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

        [DataMember(Name = "inline_query")]
        public InlineQuery InlineQuery { get; set; }

        [DataMember(Name = "chosen_inline_result")]
        public ChosenInlineResult InlineResult { get; set; }

        [DataMember(Name = "callback_query")]
        public CallbackQuery CallbackQuery { get; set; }
    }
}
