
namespace TelegramBotApi.Models
{
    using System.Runtime.Serialization;

    [DataContract]
    public class User
    {
        [DataMember(Name = "id")]
        public int Id { get; set; }

        [DataMember(Name = "first_name")]
        public string FirstName { get; set; }

        [DataMember(Name = "last_name")]
        public string LastName { get; set; }

        [DataMember(Name = "username")]
        public string UserName { get; set; }
    }
}
