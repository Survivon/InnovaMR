
namespace TelegramBotApi.Telegram.DataAccess.Request.Requests
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using Parsers;
    using Request;

    internal class ExternalRequest<T,P> : Request<T>
    {
        public new P PostContent { get; set; }

        protected override HttpRequestMessage GetRequestMessage(RequestState<T> state)
        {
            var httpRequestMessage = new HttpRequestMessage();
            switch (state.Method)
            {
                case "GET":
                    httpRequestMessage.Method = HttpMethod.Get;
                    state.Link += state.ParametersData;
                    httpRequestMessage.RequestUri = new Uri(state.Link);
                    break;

                case "POST":
                    httpRequestMessage.Method = HttpMethod.Post;
                    state.Link += state.ParametersData;
                    httpRequestMessage.RequestUri = new Uri(state.Link);

                    if (this.PostContent != null)
                    {
                        var serializedContent = JsonParser<P>.Serialize(this.PostContent);
                        httpRequestMessage.Content = new StringContent(serializedContent);
                        httpRequestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue(this.PostContentType);
                    }

                    break;
            }

            return httpRequestMessage;
        }
    }
}
