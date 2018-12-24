using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace InnovaMRBot.Models
{
    public class MergeSetting
    {
        public MergeSetting()
        {
            Reactions = new List<MessageReaction>();
            VersionedSetting = new List<VersionedMergeRequest>();
        }

        public string MrUrl { get; set; }

        public string TicketsUrl { get; set; }

        public string Description { get; set; }

        public bool IsHadAlreadyChange { get; set; }

        public int CountOfChange { get; set; }

        public string AllText { get; set; }

        public DateTimeOffset? PublishDate { get; set; }

        public List<MessageReaction> Reactions { get; set; }

        public Guid Id { get; set; }

        public string TelegramMessageId { get; set; }

        public string OwnerId { get; set; }
        
        [NotMapped]
        public User Owner { get; set; }

        public IList<VersionedMergeRequest> VersionedSetting { get; set; }

        public ConversationSetting ConversationSetting { get; set; }
    }
}
