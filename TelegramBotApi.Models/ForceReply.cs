namespace TelegramBotApi.Models
{
    using System.Runtime.Serialization;
    using Keyboard.Interface;

    [DataContract]
    public class ForceReply : IKeyboard
    {
        [DataMember(Name = "force_reply")]
        public bool IsForceReply { get; set; }

        [DataMember(Name = "selective")]
        public bool IsSelective { get; set; }
    }
}
