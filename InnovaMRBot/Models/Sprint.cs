using System;
using System.Runtime.Serialization;

namespace InnovaMRBot.Models
{
    [DataContract]
    public class Sprint
    {
        [DataMember]
        public int Number { get; set; }

        [DataMember]
        public DateTime Start { get; set; }

        [DataMember]
        public DateTime End { get; set; }
    }
}
