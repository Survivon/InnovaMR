using System;

namespace InnovaMRBot.Models
{
    public class MessageReaction
    {
        public DateTimeOffset ReactionTime { get; set; }

        public User User { get; set; }

        public string ReactionType { get; set; }

        public int ReactionInMinutes { get; set; }
    }
}
