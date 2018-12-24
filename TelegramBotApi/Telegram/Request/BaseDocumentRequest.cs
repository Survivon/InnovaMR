
using TelegramBotApi.Models.Enum;

namespace TelegramBotApi.Telegram.Request
{
    using System.Runtime.Serialization;

    [DataContract]
    public class BaseDocumentRequest : BaseRequest
    {
        [DataMember(Name = "caption")]
        public string Caption { get; set; }
        
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
    }
}
