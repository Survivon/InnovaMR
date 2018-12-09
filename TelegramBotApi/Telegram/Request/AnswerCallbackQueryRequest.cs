namespace TelegramBotApi.Telegram.Request
{
    using System.Runtime.Serialization;

    [DataContract]
    public class AnswerCallbackQueryRequest
    {
        [DataMember(Name = "callback_query_id")]
        public string CallbackId { get; set; }

        [DataMember(Name = "text")]
        public string Text { get; set; }

        [DataMember(Name = "show_alert")]
        public bool IsNeedShowAlert { get; set; }

        [DataMember(Name = "url")]
        public string Url { get; set; }
    }
}
