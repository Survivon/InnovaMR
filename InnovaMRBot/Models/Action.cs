using System;

namespace InnovaMRBot.Models
{
    public class Action
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public DateTime ExecDate { get; set; }

        public bool IsActive { get; set; }

        public string ActionMethod { get; set; }

        public bool IsMergeRequest { get; set; }

        public string MessageId { get; set; }

        public string ActionFor { get; set; }
    }
}
