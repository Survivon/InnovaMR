namespace TelegramBotApi.Models.Keyboard
{
    using System.Runtime.Serialization;
    using Game;

    [DataContract]
    public class InlineKeyboardButton
    {
        public InlineKeyboardButton()
        {
            Url = string.Empty;
        }

        [DataMember(Name = "text")]
        public string Text { get; set; }

        [DataMember(Name = "url")]
        public string Url { get; set; }

        [DataMember(Name = "callback_data")]
        public string CallbackData { get; set; }

        [DataMember(Name = "switch_inline_query")]
        public string SwitchInlineQuery { get; set; }

        [DataMember(Name = "switch_inline_query_current_chat")]
        public string SwitchInlineQueryCurrentChat { get; set; }

        [DataMember(Name = "callback_game")]
        public CallbackGame CallbackGame { get; set; }
    }
}
