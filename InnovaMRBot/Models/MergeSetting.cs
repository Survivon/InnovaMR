using System;
using System.Collections.Generic;

namespace InnovaMRBot.Models
{
    public class MergeSetting
    {
        public MergeSetting()
        {
            Reactions = new List<MessageReaction>();
            TicketsUrl = new List<string>();
            VersionedSetting = new List<VersionedMergeRequest>();
        }

        public string MrUrl { get; set; }

        public List<string> TicketsUrl { get; set; }

        public string Description { get; set; }

        public bool IsHadAlreadyChange { get; set; }

        public int CountOfChange { get; set; }

        public DateTimeOffset? PublishDate { get; set; }

        public List<MessageReaction> Reactions { get; set; }

        public string Id { get; set; }

        public User Owner { get; set; }

        public List<VersionedMergeRequest> VersionedSetting { get; set; }
    }
}
