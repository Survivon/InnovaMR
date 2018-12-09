namespace TelegramBotApi.Models
{
    using System.Runtime.Serialization;
    using Enum;

    public class Chat
    {
        [DataMember(Name = "id")]
        public int Id { get; set; }

        [DataMember(Name = "type")]
        public ChatType Type { get; set; }

        [DataMember(Name = "title")]
        public string Title { get; set; }

        [DataMember(Name = "first_name")]
        public string FirstName { get; set; }

        [DataMember(Name = "last_name")]
        public string LastName { get; set; }

        [DataMember(Name = "username")]
        public string UserName { get; set; }

        [DataMember(Name = "all_members_are_administrators")]
        public bool IsAllMembersAreAdministrator { get; set; }
    }
}
