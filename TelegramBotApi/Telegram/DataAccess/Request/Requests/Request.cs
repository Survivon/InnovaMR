namespace TelegramBotApi.Telegram.DataAccess.Request.Requests
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using Parsers;
    using Request;

    internal class Request<T>
    {
        private RequestState<T> State { get; set; }

        public ParametersCollection Parameters { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Need chnage during logic.")]
        public CookieCollection Cookies { get; set; }

        public string Method { get; set; }

        public object PostContent { get; set; }

        public string PostContentType { get; set; }

        public Dictionary<string, string> Headers { get; set; }

        public Request()
        {
            this.Method = "GET";
            this.Headers = new Dictionary<string, string>();
            this.Parameters = new ParametersCollection();
            this.Cookies = new CookieCollection();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public async Task<Response<T>> ExecuteAsync(string link)
        {
            this.State = new RequestState<T> {Link = link, Method = this.Method, Cookies = this.Cookies, ParametersData = this.GetParametersData()};

            return await this.SetupRequestAsync(this.State);
        }

        private string GetParametersData()
        {
            var linkBuilder = new StringBuilder();

            if (this.Parameters.Any())
            {
                if (!this.State.Link.Contains("?"))
                {
                    var lastLinkSymbol = this.State.Link[this.State.Link.Length - 1];
                    if (lastLinkSymbol == '/')
                    {
                        this.State.Link = this.State.Link.Substring(0, this.State.Link.Length - 1);
                    }

                    linkBuilder.Append("?");
                }
                else
                {
                    linkBuilder.Append("&");
                }

                foreach (var parameter in this.Parameters)
                {
                    if (!string.IsNullOrEmpty(parameter.Value))
                    {
                        var escapedValue = Uri.EscapeDataString(parameter.Value);
                        linkBuilder.AppendFormat(CultureInfo.InvariantCulture, "{0}={1}&", parameter.Key, escapedValue);
                    }
                }

                linkBuilder.Remove(linkBuilder.Length - 1, 1);
            }

            return linkBuilder.ToString();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Validated by Guard")]
        private async Task<Response<T>> SetupRequestAsync(RequestState<T> state)
        {
            var responseData = new Response<T>();

            using (var handler = new HttpClientHandler())
            {
                handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                handler.UseCookies = state.Cookies != null;
                if (handler.UseCookies)
                {
                    var cookieContainer = new CookieContainer();
                    cookieContainer.Add(new Uri(state.Link), state.Cookies);

                    handler.CookieContainer = cookieContainer;
                }

                using (var httpClient = new HttpClient(handler))
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(60);
                    foreach (var header in this.Headers)
                    {
                        try
                        {
                            httpClient.DefaultRequestHeaders.Add(header.Key.ToString(), header.Value);
                        }
                        catch (Exception ex)
                        {
                            
                        }
                    }

                    HttpResponseMessage httpResponseMessage = null;
                    using (var httpRequestMessage = this.GetRequestMessage(state))
                    {
                        httpResponseMessage = await this.GetResponseMessage(httpClient, httpRequestMessage, state);
                    }

                    if (httpResponseMessage != null)
                    {
                        using (httpResponseMessage)
                        {
                            var response = await this.GetStringResponseAsync(httpResponseMessage);
                            if (httpResponseMessage.IsSuccessStatusCode)
                            {
                                if (!string.IsNullOrEmpty(response))
                                {
                                    responseData = JsonParser<T>.Parse(response);
                                }
                                else
                                {
                                    responseData.Error = new RequestError { Type = ErrorType.Unknown };
                                }
                            }
                            else
                            {
                                var errorType = ErrorType.Network;
                                if (httpResponseMessage.StatusCode == HttpStatusCode.Unauthorized)
                                {
                                    errorType = ErrorType.Unauthorized;
                                }

                                responseData.Error = new RequestError { Type = errorType, Code = (int)httpResponseMessage.StatusCode, Message = response };
                            }
                        }
                    }
                    else
                    {
                        responseData.Error = new RequestError { Type = ErrorType.Network };
                    }
                }
            }

            return responseData;
        }

        private async Task<string> GetStringResponseAsync(HttpResponseMessage httpResponseMessage)
        {
            var result = string.Empty;

            if (httpResponseMessage.Content != null)
            {
                using (var responseStream = await httpResponseMessage.Content.ReadAsStreamAsync())
                {
                    if (responseStream != null)
                    {
                        using (var reader = new StreamReader(responseStream))
                        {
                            result = await reader.ReadToEndAsync();
                        }
                    }
                }
            }

            return result;
        }

        protected virtual HttpRequestMessage GetRequestMessage(RequestState<T> state)
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
                        var bytes = this.PostContent as byte[];
                        var dictionary = this.PostContent as IEnumerable<KeyValuePair<string, string>>;
                        if (bytes != null)
                        {
                            if (bytes.Length > 0)
                            {
                                httpRequestMessage.Content = new ByteArrayContent(bytes);
                            }
                        }
                        else if (dictionary != null)
                        {
                            httpRequestMessage.Content = new FormUrlEncodedContent(dictionary);
                        }

                        httpRequestMessage.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(this.PostContentType);
                    }

                    break;
            }

            return httpRequestMessage;
        }

        private async Task<HttpResponseMessage> GetResponseMessage(HttpClient httpClient, HttpRequestMessage httpRequestMessage, RequestState<T> state)
        {
            HttpResponseMessage httpResponseMessage = null;
            try
            {
                httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);
            }
            catch (TaskCanceledException) { /* ignored */ }
            catch (HttpRequestException) { /* ignored */ }
            catch (Exception ex)
            {
                
            }

            return httpResponseMessage;
        }
    }
}
