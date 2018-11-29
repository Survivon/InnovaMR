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
    }
}
