
using System.Collections.Generic;

namespace InnovaMRBot.Models
{
    public class ConversationSetting
    {
        public ConversationSetting()
        {
            ListOfMerge = new List<MergeSetting>();
        }

        public bool IsMergeRequestConversation { get; set; }

        public string ConversationId { get; set; }

        public bool CanWriteToConversation { get; set; }

        public List<MergeSetting> ListOfMerge { get; set; }
    }
}
