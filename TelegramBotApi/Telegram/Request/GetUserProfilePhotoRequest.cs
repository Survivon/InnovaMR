namespace TelegramBotApi.Telegram.Request
{
    using System.Runtime.Serialization;

    [DataContract]
    public class GetUserProfilePhotoRequest
    {
        [DataMember(Name = "user_id")]
        public int UserId { get; set; }

        [DataMember(Name = "offset")]
        public int? Offset { get; set; }

        [DataMember(Name = "limit")]
        public int? Limit { get; set; }
    }
}
