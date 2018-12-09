namespace TelegramBotApi.Models.Keyboard
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using Interface;

    [DataContract]
    public class ReplyKeyboardMarkup : IKeyboard
    {
        [DataMember(Name = "keyboard")]
        public List<List<KeyboardButton>> Keyboard { get; set; }

        [DataMember(Name = "resize_keyboard")]
        public bool IsResizeKeyboard { get; set; }

        [DataMember(Name = "one_time_keyboard")]
        public bool IsHideKeyboardAfterClick { get; set; }

        [DataMember(Name = "selective")]
        public bool IsSelectiveShow { get; set; }
    }
}
