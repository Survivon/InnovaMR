
namespace TelegramBotApi.Telegram.Request
{
    using System.Runtime.Serialization;

    [DataContract]
    public class BaseDocumentRequest : BaseRequest
    {
        [DataMember(Name = "caption")]
        public string Caption { get; set; }
    }
}
