namespace InnovaMRBot.Models
{
    public class ResponseMessage
    {
        public ChatSetting ConversationId { get; set; }

        public string Message { get; set; }

        public string ReplyMessageId { get; set; }
    }
}
