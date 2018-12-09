namespace TelegramBotApi.Models.Keyboard
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using Interface;

    [DataContract]
    public class InlineKeyboardMarkup : IKeyboard
    {
        [DataMember(Name = "inline_keyboard")]
        public List<List<InlineKeyboardButton>> InlineKeyboardButtons { get; set; }
    }
}
