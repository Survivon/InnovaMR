using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InnovaMRBot.Models
{
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
    }
}
