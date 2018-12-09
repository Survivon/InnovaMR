namespace TelegramBotApi.Telegram.Request
{
    using System.Runtime.Serialization;
    using Models;
    using Models.Keyboard.Interface;

    [DataContract]
    public class EditReplyMarkupRequest
    {
        public string ChatId { get; set; }

        [DataMember(Name = "message_id")]
        public string EditMessageId { get; set; }

        [DataMember(Name = "inline_message_id")]
        public string InlineMessageId { get; set; }
        
        // WARNING! Doesn't remove default value
        [DataMember(Name = "reply_markup")]
        public IKeyboard ReplyMarkup { get; set; } = new ForceReply();
    }
}
