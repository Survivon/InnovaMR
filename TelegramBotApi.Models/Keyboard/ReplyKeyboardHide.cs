namespace TelegramBotApi.Models.Keyboard
{
    using System.Runtime.Serialization;
    using Interface;

    [DataContract]
    public class ReplyKeyboardHide : IKeyboard
    {
        [DataMember(Name = "remove_keyboard")]
        public bool IsHideKeyboard { get; set; }

        [DataMember(Name = "selective")]
        public bool IsSelective { get; set; }
    }
}
