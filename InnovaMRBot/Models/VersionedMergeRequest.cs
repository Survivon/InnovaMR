using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace InnovaMRBot.Models
{
    [Table("VersionedMergeRequest")]
    public class VersionedMergeRequest
    {
        public VersionedMergeRequest()
        {
            Reactions = new List<MessageReaction>();
        }

        public string OwnerMergeId { get; set; }

        public string Id { get; set; }
        
        public DateTimeOffset? PublishDate { get; set; }

        public List<MessageReaction> Reactions { get; set; }

        public string AllDescription { get; set; }

        public string Description { get; set; }

        public MergeSetting MergeSetting { get; set; }
    }
}
