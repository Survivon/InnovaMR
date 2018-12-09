namespace TelegramBotApi.Telegram.Request
{
    using System.Runtime.Serialization;
    using Models;
    using Models.Keyboard.Interface;

    [DataContract]
    public class BaseRequest
    {
        [DataMember(Name = "chat_id")]
        public string ChatId { get; set; }

        /// <summary>
        /// Sends the message silently. iOS users will not receive a notification, Android users will receive a notification with no sound.
        /// </summary>
        [DataMember(Name = "disable_notification")]
        public bool IsDisableNotification { get; set; }

        [DataMember(Name = "reply_to_message_id")]
        public int? ReplyMessageId { get; set; }

        // WARNING! Doesn't remove default value
        [DataMember(Name = "reply_markup")]
        public IKeyboard ReplyMarkup { get; set; } = new ForceReply();
    }
}
