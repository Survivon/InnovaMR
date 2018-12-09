
namespace TelegramBotApi.Telegram.Request
{
    using System.Runtime.Serialization;

    [DataContract]
    public class SendStickerRequest : BaseRequest
    {
        [DataMember(Name = "sticker")]
        public string Sticker { get; set; }
    }
}
