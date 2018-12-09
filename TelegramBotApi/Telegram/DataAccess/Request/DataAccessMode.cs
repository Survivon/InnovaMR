namespace TelegramBotApi.Telegram.DataAccess.Request
{
    /// <summary>
    /// Source of data.
    /// </summary>
    public enum DataAccessMode
    {
        /// <summary>
        /// Get data only from cache.
        /// </summary>
        Cache,

        /// <summary>
        /// Check if cache is expired, read data from server.
        /// </summary>
        CacheOrServer,

        /// <summary>
        /// Read data from server. If request failed, try read from cache.
        /// </summary>
        ServerOrCache,

        /// <summary>
        /// Read data from server only.
        /// </summary>
        Server,
    }
}
