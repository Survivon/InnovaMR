namespace TelegramBotApi.Telegram.DataAccess.Request
{
    public enum ErrorType
    {
        Unknown = 0,

        Network = 1,

        FileSystem = 2,

        Parsing = 3,

        Authentication = 4,

        NotModified = 5,

        TaskCanceled = 6,

        Unauthorized = 7,

        EmptyParameters = 8,
    }
}
