using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

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
