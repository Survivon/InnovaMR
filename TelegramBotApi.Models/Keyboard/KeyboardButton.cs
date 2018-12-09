namespace TelegramBotApi.Models.Keyboard
{
    using System.Runtime.Serialization;

    [DataContract]
    public class KeyboardButton
    {
        [DataMember(Name = "text")]
        public string Text { get; set; }

        [DataMember(Name = "request_contact")]
        public bool IsRequestContact { get; set; }

        [DataMember(Name = "request_location")]
        public bool IsRequestLocation { get; set; }
    }
}
