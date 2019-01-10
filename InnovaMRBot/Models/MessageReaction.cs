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

        public string Type { get; set; }

        [NotMapped]
        public ReactionType ReactionType
        {
            get => Type.Equals(ReactionType.DisLike.ToString()) ? ReactionType.DisLike : ReactionType.Like;
            set { Type = value.ToString(); }
        }

        [NotMapped]
        public User User { get; set; }

        public int ReactionInMinutes { get; set; }

        public MergeSetting MergeSetting { get; set; }

        public VersionedMergeRequest VersionedMergeRequest { get; set; }

        public MessageReaction SetReactionInMinutes(DateTimeOffset publishDate)
        {
            var reaction = (int)ReactionTime.Subtract(publishDate).TotalMinutes;
            ReactionInMinutes = reaction == 0 ? 1 : reaction;
            return this;
        }
    }
}
