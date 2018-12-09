namespace TelegramBotApi.Models.InlineQueryResults
{
    using System.Runtime.Serialization;
    using InputMessageContent;
    using Keyboard;

    [DataContract]
    public class InlineQueryResult
    {
        [DataMember(Name = "type")]
        public string Type { get; protected set; }

        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "reply_markup")]
        public InlineKeyboardMarkup InlineKeyboardMarkup { get; set; }

        [DataMember(Name = "input_message_content")]
        public InputMessageContent InputMessageContent { get; set; }
    }
}
