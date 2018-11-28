using Microsoft.Bot.Builder;

namespace InnovaMRBot.Helpers
{
    public class CustomConversationState : ConversationState
    {
        public IStorage Storage { get; private set; }

        public CustomConversationState(IStorage storage)
            : base(storage)
        {
            Storage = storage;
        }
    }
}
