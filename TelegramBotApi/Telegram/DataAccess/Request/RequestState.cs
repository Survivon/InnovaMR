namespace TelegramBotApi.Telegram.DataAccess.Request
{
    using System.Net;

    internal class RequestState<T>
    {
        public string Link { get; set; }

        public string ParametersData { get; set; }

        public string Method { get; set; }

        public CookieCollection Cookies { get; set; }
    }
}
