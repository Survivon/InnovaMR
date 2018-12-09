namespace TelegramBotApi.Models.InputMessageContent
{
    using System.Runtime.Serialization;
    using Enum;

    [DataContract]
    public class InputTextMessageContent : InputMessageContent
    {
        [DataMember(Name = "message_text")]
        public string MessageText { get; set; }

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
                        return "markdown-style";
                    case FormattingMessageType.HTML:
                        return "HTML-style";
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
