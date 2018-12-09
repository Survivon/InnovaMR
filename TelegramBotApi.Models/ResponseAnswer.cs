namespace TelegramBotApi.Models
{
    using System.Runtime.Serialization;

    [DataContract]
    public class ResponseAnswer<T>
    {
        [DataMember(Name = "ok")]
        public bool IsSuccess { get; set; }

        [DataMember(Name = "result")]
        public T Result { get; set; }
        
        [DataMember(Name = "error_code")]
        public int ErrorCode { get; set; }
    }
}
