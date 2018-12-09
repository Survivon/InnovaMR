namespace TelegramBotApi.Telegram.Request
{
    using System.Runtime.Serialization;
    using Models.Enum;

    [DataContract]
    public class SendBotActionRequest
    {
        [DataMember(Name = "chat_id")]
        public string ChatId { get; set; }

        [IgnoreDataMember]
        public BotActionType BotActionType { get; set; }

        [DataMember(Name = "action")]
        public string BotAction
        {
            get
            {
                switch (this.BotActionType)
                {
                    case BotActionType.Typing:
                        return "typing";
                    case BotActionType.UploadPhoto:
                        return "upload_photo";
                    case BotActionType.UploadVideo:
                        return "upload_video";
                    case BotActionType.UploadAudio:
                        return "upload_audio";
                    case BotActionType.UploadDocument:
                        return "upload_document";
                    case BotActionType.FindLocation:
                        return "find_location";
                    default:
                        return "";
                }
            }
        }
    }
}
