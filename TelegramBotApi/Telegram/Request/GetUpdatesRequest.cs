namespace TelegramBotApi.Telegram.Request
{
    using System.Runtime.Serialization;

    [DataContract]
    public class GetUpdatesRequest
    {
        [DataMember(Name = "offset")]
        public int? Offset { get; set; }

        /// <summary>
        /// Limits the number of updates to be retrieved. Values between 1—100 are accepted. Defaults to 100.
        /// </summary>
        [DataMember(Name = "limit")]
        public int LimitOfUpdates { get; set; } = 100;

        /// <summary>
        /// Timeout in seconds for long polling. Defaults to 0, i.e. usual short polling
        /// </summary>
        [DataMember(Name = "timeout")]
        public int TimeoutOfUpdate { get; set; } = 0;
    }
}
