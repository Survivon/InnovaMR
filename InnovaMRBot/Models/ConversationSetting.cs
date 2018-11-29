using System;
using System.Collections.Generic;

namespace InnovaMRBot.Models
{
    public class ConversationSetting
    {
        public ConversationSetting()
        {
            ListOfMerge = new List<MergeSetting>();
            Id = Guid.NewGuid();
            Admins = new List<User>();
        }

        public Guid Id { get; set; }

        public ChatSetting AlertChat { get; set; }

        public ChatSetting MRChat { get; set; }

        public List<User> Admins { get; set; }

        public List<MergeSetting> ListOfMerge { get; set; }
    }
}
