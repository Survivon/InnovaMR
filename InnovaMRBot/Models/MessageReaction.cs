using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace InnovaMRBot.Models
{
    [Table("MessageReaction")]
    public class MessageReaction
    {
        public Guid Id { get; set; }

        public DateTimeOffset ReactionTime { get; set; }

        public string UserId { get; set; }

        [NotMapped]
        public User User { get; set; }

        public int ReactionInMinutes { get; set; }

        public MergeSetting MergeSetting { get; set; }

        public VersionedMergeRequest VersionedMergeRequest { get; set; }

        public MessageReaction SetReactionInMinutes(DateTimeOffset publishDate)
        {
            var reaction = (int) ReactionTime.Subtract(publishDate).TotalMinutes;
            ReactionInMinutes = reaction == 0 ? 1 : reaction;
            return this;
        }
    }
}
