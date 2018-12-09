namespace TelegramBotApi.Telegram.DataAccess.Request
{
    public class RequestError
    {
        public ErrorType Type { get; set; }

        public int Code { get; set; }

        public string Message { get; set; }
    }
}
