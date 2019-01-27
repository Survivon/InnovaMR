using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace InnovaMRBot.Models
{
    public class ChatSetting
    {
        public ChatSetting()
        {
            Sprints = new List<Sprint>();
        }

        public string Id { get; set; }

        public bool IsAlertChat { get; set; }

        public bool IsMRChat { get; set; }

        public Guid SyncId { get; set; }

        public string Name { get; set; }

        public ConversationSetting ConversationSettingMrChat { get; set; }

        //public ConversationSetting ConversationSettingAlertChat { get; set; }

        //public Guid? AlertChatId { get; set; }

        public Guid MRChatId { get; set; }

        public string SprintInfo
        {
            get => JsonConvert.SerializeObject(Sprints ?? new List<Sprint>());
            set => Sprints = JsonConvert.DeserializeObject<List<Sprint>>(value ?? string.Empty) ?? new List<Sprint>();
        }

        [NotMapped]
        public List<Sprint> Sprints { get; set; }
    }
}
