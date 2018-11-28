
using System.Collections.Generic;

namespace InnovaMRBot.Models
{
    public class Conversations
    {
        public Conversations()
        {
            BotConversation = new List<ConversationSetting>();
        }

        public List<ConversationSetting> BotConversation { get; set; }
    }
}
