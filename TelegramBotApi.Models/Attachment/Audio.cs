
namespace TelegramBotApi.Models.Attachment
{
    using System.Runtime.Serialization;

    [DataContract]
    public class Audio : BaseRecorder
    {
        [DataMember(Name = "performer")]
        public string Performer { get; set; }

        [DataMember(Name = "title")]
        public string Title { get; set; }
    }
}
