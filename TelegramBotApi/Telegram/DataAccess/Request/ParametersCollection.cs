namespace TelegramBotApi.Telegram.DataAccess.Request
{
    using System.Collections;
    using System.Collections.Generic;

    internal class ParametersCollection : IEnumerable<KeyValuePair<string, string>>
    {
        private List<KeyValuePair<string, string>> collection { get; set; }

        public ParametersCollection()
        {
            this.collection = new List<KeyValuePair<string, string>>();
        }

        public void Add(string key, string value)
        {
            this.collection.Add(new KeyValuePair<string, string>(key, value));
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return this.collection.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)this.collection).GetEnumerator();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Justification = "does not meet the business logic of the application")]
        public static implicit operator ParametersCollection(Dictionary<string, string> dictionary)
        {
            var result = new ParametersCollection();

            if (dictionary != null)
            {
                foreach (var item in dictionary)
                {
                    result.Add(item.Key, item.Value);
                }
            }

            return result;
        }
    }
}
