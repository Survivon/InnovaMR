
namespace TelegramBotApi.Telegram.DataAccess.Parsers
{
    using Request;
    using Newtonsoft.Json;
    using System;

    internal static class JsonParser<T>
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "Hangouts.Core.Request.RequestError.set_Message(System.String)", Justification = "This message only for local developers")]
        public static Response<T> Parse(string response)
        {
            var result = new Response<T>();
            if (!string.IsNullOrWhiteSpace(response))
            {
                try
                {
                    result.Result = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(response);
                }
                catch (Exception ex)
                {
                    result.Error = new RequestError()
                    {
                        Type = ErrorType.Parsing,
                    };
                }
            }
            else
            {
                result.Error = new RequestError()
                {
                    Type = ErrorType.FileSystem,
                };
            }

            return result;
        }

        public static string Serialize(object data)
        {
            var result = string.Empty;
            if (data != null)
            {
                try
                {
                    var settings = new JsonSerializerSettings() { ContractResolver = new NullToEmptyStringResolver() };
                    result = JsonConvert.SerializeObject(data, settings);
                }
                catch (Exception) { /* ignore */ }
            }

            return result;
        }
    }
}
