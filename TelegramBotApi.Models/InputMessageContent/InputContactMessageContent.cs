namespace TelegramBotApi.Models.InputMessageContent
{
    using System.Runtime.Serialization;

    [DataContract]
    public class InputContactMessageContent : InputMessageContent
    {
        [DataMember(Name = "phone_number")]
        public string PhoneNumber { get; set; }

        [DataMember(Name = "first_name")]
        public string FirstName { get; set; }

        [DataMember(Name = "last_name")]
        public string LastName { get; set; }
    }
}
