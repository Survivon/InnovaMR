using System;

namespace InnovaMRBot.Models
{
    public class ChatSetting
    {
        public string Id { get; set; }

        public bool IsAlertChat { get; set; }

        public bool IsMRChat { get; set; }

        public Guid SyncId { get; set; }

        public string Name { get; set; }

        public ConversationSetting ConversationSettingMrChat { get; set; }

        //public ConversationSetting ConversationSettingAlertChat { get; set; }

        //public Guid? AlertChatId { get; set; }

        public Guid MRChatId { get; set; }
    }
}
