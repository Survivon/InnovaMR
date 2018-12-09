namespace TelegramBotApi.Telegram.DataAccess.Request
{
    public class Response<T>
    {
        public T Result { get; set; }

        public RequestError Error { get; set; }

        public DataAccessMode ResultSource { get; set; }

        public bool IsSuccess
        {
            get
            {
                return this.Error == null;
            }
        }
    }
}
