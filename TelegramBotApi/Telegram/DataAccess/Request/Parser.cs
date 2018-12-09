namespace TelegramBotApi.Telegram.DataAccess.Request
{
    internal abstract class Parser<T>
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Async pattern")]
        public virtual Response<T> Parse(string response)
        {
            return this.Parse(response);
        }

        public virtual Response<T> Parse(object[] data)
        {
            return this.Parse(data);
        }

        public virtual string Serialize(object data)
        {
            return this.Serialize(data);
        }
    }
}
