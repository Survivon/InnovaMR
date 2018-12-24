namespace TelegramBotApi.Telegram.Request
{
    using System.Runtime.Serialization;
    using Models.Enum;

    [DataContract]
    public class SendMessageRequest : BaseRequest
    {
        [DataMember(Name = "text")]
        public string Text { get; set; }

        [DataMember(Name = "parse_mode")]
        public string FormattingStyle
        {
            get
            {
                switch (this.FormattingMessageType)
                {
                    case FormattingMessageType.Default:
                        return string.Empty;
                    case FormattingMessageType.Markdown:
                        return "markdown";
                    case FormattingMessageType.HTML:
                        return "HTML";
                    default:
                        return string.Empty;
                }
            }
        }

        [IgnoreDataMember]
        public FormattingMessageType FormattingMessageType { get; set; }

        [DataMember(Name = "disable_web_page_preview")]
        public bool IsDisableWebPagePreview { get; set; }
    }
}
