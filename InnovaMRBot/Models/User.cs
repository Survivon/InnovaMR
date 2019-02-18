using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using InnovaMRBot.Models.Enum;

namespace InnovaMRBot.Models
{
    public class User
    {
        public User()
        {
            Commands = new List<CommandCollection>();
        }

        public string UserId { get; set; }

        public string Name { get; set; }

        public bool IsAdmin { get; set; }

        public string SkypeLogin { get; set; }

        public string ChatId { get; set; }

        public bool CanRemoveOldMr { get; set; }

        public long TimeDiff { get; set; }

        [NotMapped]
        public UserRole Role
        {
            get
            {
                if (string.IsNullOrEmpty(RoleInfo))
                {
                    return UserRole.Default;
                }

                if (RoleInfo.Equals(UserRole.Dev.ToString()))
                {
                    return UserRole.Dev;
                }
                else if (RoleInfo.Equals(UserRole.QA.ToString()))
                {
                    return UserRole.QA;
                }
                else if (RoleInfo.Equals(UserRole.PM.ToString()))
                {
                    return UserRole.PM;
                }
                else
                {
                    return UserRole.Default;
                }
            }
            set => RoleInfo = value.ToString();
        }

        public string RoleInfo { get; set; }

        public string CommandsInfo
        {
            get => JsonConvert.SerializeObject(Commands ?? new List<CommandCollection>());
            set => Commands = JsonConvert.DeserializeObject<List<CommandCollection>>(value ?? string.Empty) ?? new List<CommandCollection>();
        }

        [NotMapped]
        public List<CommandCollection> Commands { get; set; }

        public ConversationSetting Conversation { get; set; }
    }
}
