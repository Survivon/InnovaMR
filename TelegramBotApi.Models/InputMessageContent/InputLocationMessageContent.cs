namespace TelegramBotApi.Models.InputMessageContent
{
    using System.Runtime.Serialization;

    [DataContract]
    public class InputLocationMessageContent : InputMessageContent
    {
        [DataMember(Name = "latitude")]
        public float Latitude { get; set; }

        [DataMember(Name = "longitude")]
        public float Longitude { get; set; }
    }
}
