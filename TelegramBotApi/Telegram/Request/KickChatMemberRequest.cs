namespace TelegramBotApi.Telegram.Request
{
    using System.Runtime.Serialization;

    [DataContract]
    public class KickChatMemberRequest
    {
        [DataMember(Name = "chat_id")]
        public string ChatId { get; set; }

        [DataMember(Name = "user_id")]
        public int UserId { get; set; }
    }
}
