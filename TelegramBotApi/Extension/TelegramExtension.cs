
namespace TelegramBotApi.Extension
{
    using Models;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Threading.Tasks;
    using Telegram;
    using Telegram.DataAccess.Parsers;
    using Telegram.DataAccess.Request;
    using Telegram.DataAccess.Request.Requests;
    using Telegram.Request;
    using File = Models.Attachment.File;

    public static class TelegramExtension
    {
        private static int offset = 0;
        private const string POST_METHOD = "POST";
        private const string GET_METHOD = "GET";

        public static async Task<User> GetMeAsync(this Telegram telegram)
        {
            var result = new User();

            var url = telegram.GetFullPathUrl("getMe");

            var request = new ExternalRequest<ResponseAnswer<User>, object>()
            {
                Method = POST_METHOD,
                PostContentType = "application/json"
            };

            var response = await RequestSender.Execute(DataAccessMode.Server, request, url).ConfigureAwait(false);

            result = response.Result.Result;

            return result;
        }

        public static async Task<Message> SendMessageAsync(this Telegram telegram, SendMessageRequest sendMessageRequest)
        {
            var result = new Message();

            var url = telegram.GetFullPathUrl("sendMessage");

            var request = new ExternalRequest<ResponseAnswer<Message>, SendMessageRequest>()
            {
                Method = POST_METHOD,
                PostContentType = "application/json",
                PostContent = sendMessageRequest
            };

            var response = await RequestSender.Execute(DataAccessMode.Server, request, url).ConfigureAwait(false);

            result = response?.Result?.Result ?? null;

            return result;
        }

        public static async Task<bool> RemoveMessageAsync(this Telegram telegram,
            RemoveMessageRequest removeMessageRequest)
        {
            var result = false;

            var url = telegram.GetFullPathUrl("deleteMessage");

            var request = new ExternalRequest<ResponseAnswer<bool>, RemoveMessageRequest>()
            {
                Method = POST_METHOD,
                PostContentType = "application/json",
                PostContent = removeMessageRequest
            };

            var response = await RequestSender.Execute(DataAccessMode.Server, request, url).ConfigureAwait(false);

            result = response.Result.Result;

            return result;
        }

        public static async Task<List<Update>> GetUpdatesAsync(this Telegram telegram)
        {
            var result = new List<Update>();

            var url = telegram.GetFullPathUrl("getUpdates");

            var request = new ExternalRequest<ResponseAnswer<List<Update>>, GetUpdatesRequest>()
            {
                Method = POST_METHOD,
                PostContentType = "application/json",
            };

            if (offset != 0)
            {
                request.PostContent = new GetUpdatesRequest() { Offset = offset };
            }

            var response = await RequestSender.Execute(DataAccessMode.Server, request, url).ConfigureAwait(false);

            result = response?.Result?.Result ?? new List<Update>();

            if (result.Any())
            {
                offset = result.LastOrDefault().Id + 1;
            }

            return result;
        }
        
        public static async Task SetWebhookAsync(this Telegram telegram, string webhookUrl)
        {
            var webhookRequest = new WebhookEntity()
            {
                Url = webhookUrl
            };

            var url = telegram.GetFullPathUrl("setWebhook");

            var request = new ExternalRequest<ResponseAnswer<object>, WebhookEntity>()
            {
                Method = POST_METHOD,
                PostContentType = "application/json",
                PostContent = webhookRequest
            };

            var response = await RequestSender.Execute(DataAccessMode.Server, request, url).ConfigureAwait(false);
        }

        public static async Task<WebhookInfoEntity> GetWebhookInfoAsync(this Telegram telegram)
        {
            var url = telegram.GetFullPathUrl("getWebhookInfo");


            var request = new ExternalRequest<ResponseAnswer<WebhookInfoEntity>, object>()
            {
                Method = POST_METHOD,
                PostContentType = "application/json"
            };

            var result = await RequestSender.Execute(DataAccessMode.Server, request, url).ConfigureAwait(false);

            return result.Result.Result;
        }

        public static async Task<Message> SendPhotoAsync(this Telegram telegram, SendPhotoRequest sendRequest, string file = null)
        {
            var result = new Message();

            if (string.IsNullOrEmpty(file) && string.IsNullOrEmpty(sendRequest.Photo))
            {
                return null;
            }

            var url = telegram.GetFullPathUrl("sendPhoto");

            using (var form = new MultipartFormDataContent())
            {
                AddParametersToRequest(form, sendRequest);
                using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    if (!string.IsNullOrEmpty(file))
                    {
                        form.Add(new StreamContent(fileStream), "photo", Path.GetFileName(file));
                    }

                    using (var client = new HttpClient())
                    {
                        var responseHendler = await client.PostAsync(url, form);
                        if (responseHendler != null)
                        {
                            var response = await responseHendler.Content.ReadAsStringAsync();
                            if (!string.IsNullOrEmpty(response))
                            {
                                result = Newtonsoft.Json.JsonConvert.DeserializeObject<ResponseAnswer<Message>>(response).Result;
                            }
                        }
                    }
                }
            }

            return result;
        }

        public static async Task<Message> SendAudioAsync(this Telegram telegram, SendAudioRequest sendRequest, string file = null)
        {
            var result = new Message();

            if (string.IsNullOrEmpty(file) 
                && string.IsNullOrEmpty(sendRequest.Audio))
            {
                return null;
            }

            if (string.IsNullOrEmpty(sendRequest.Audio) && !string.IsNullOrEmpty(file) && Path.GetExtension(file) != ".mp3")
            {
                return null;
            }

            var url = telegram.GetFullPathUrl("sendAudio");

            using (var form = new MultipartFormDataContent())
            {
                AddParametersToRequest(form, sendRequest);
                using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    if (!string.IsNullOrEmpty(file))
                    {
                        form.Add(new StreamContent(fileStream), "audio", Path.GetFileName(file));
                    }

                    using (var client = new HttpClient())
                    {
                        var responseHendler = await client.PostAsync(url, form);
                        if (responseHendler != null)
                        {
                            var response = await responseHendler.Content.ReadAsStringAsync();
                            if (!string.IsNullOrEmpty(response))
                            {
                                result = Newtonsoft.Json.JsonConvert.DeserializeObject<ResponseAnswer<Message>>(response).Result;
                            }
                        }
                    }
                }
            }

            return result;
        }

        public static async Task<Message> SendDocumentAsync(this Telegram telegram, SendDocumentRequest sendRequest, string file = null)
        {
            var result = new Message();

            if (string.IsNullOrEmpty(file)
                && string.IsNullOrEmpty(sendRequest.Document))
            {
                return null;
            }
            
            var url = telegram.GetFullPathUrl("sendDocument");

            using (var form = new MultipartFormDataContent())
            {
                AddParametersToRequest(form, sendRequest);
                using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    if (!string.IsNullOrEmpty(file))
                    {
                        form.Add(new StreamContent(fileStream), "document", Path.GetFileName(file));
                    }

                    using (var client = new HttpClient())
                    {
                        var responseHendler = await client.PostAsync(url, form);
                        if (responseHendler != null)
                        {
                            var response = await responseHendler.Content.ReadAsStringAsync();
                            if (!string.IsNullOrEmpty(response))
                            {
                                result = Newtonsoft.Json.JsonConvert.DeserializeObject<ResponseAnswer<Message>>(response).Result;
                            }
                        }
                    }
                }
            }

            return result;
        }

        public static async Task<Message> SendStickerAsync(this Telegram telegram, SendStickerRequest sendRequest, string file = null)
        {
            var result = new Message();

            if (string.IsNullOrEmpty(file)
                && string.IsNullOrEmpty(sendRequest.Sticker))
            {
                return null;
            }

            var url = telegram.GetFullPathUrl("sendSticker");

            using (var form = new MultipartFormDataContent())
            {
                AddParametersToRequest(form, sendRequest);
                using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    if (!string.IsNullOrEmpty(file))
                    {
                        form.Add(new StreamContent(fileStream), "sticker", Path.GetFileName(file));
                    }

                    using (var client = new HttpClient())
                    {
                        var responseHendler = await client.PostAsync(url, form);
                        if (responseHendler != null)
                        {
                            var response = await responseHendler.Content.ReadAsStringAsync();
                            if (!string.IsNullOrEmpty(response))
                            {
                                result = Newtonsoft.Json.JsonConvert.DeserializeObject<ResponseAnswer<Message>>(response).Result;
                            }
                        }
                    }
                }
            }

            return result;
        }

        public static async Task<Message> SendVideoAsync(this Telegram telegram, SendVideoRequest sendRequest, string file = null)
        {
            var result = new Message();

            if (string.IsNullOrEmpty(file)
                && string.IsNullOrEmpty(sendRequest.Video))
            {
                return null;
            }

            if (string.IsNullOrEmpty(sendRequest.Video) && !string.IsNullOrEmpty(file) && Path.GetExtension(file) != ".mp4")
            {
                return null;
            }

            var url = telegram.GetFullPathUrl("sendVideo");

            using (var form = new MultipartFormDataContent())
            {
                AddParametersToRequest(form, sendRequest);
                using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    if (!string.IsNullOrEmpty(file))
                    {
                        form.Add(new StreamContent(fileStream), "video", Path.GetFileName(file));
                    }

                    using (var client = new HttpClient())
                    {
                        var responseHendler = await client.PostAsync(url, form);
                        if (responseHendler != null)
                        {
                            var response = await responseHendler.Content.ReadAsStringAsync();
                            if (!string.IsNullOrEmpty(response))
                            {
                                result = Newtonsoft.Json.JsonConvert.DeserializeObject<ResponseAnswer<Message>>(response).Result;
                            }
                        }
                    }
                }
            }

            return result;
        }

        public static async Task<Message> SendVoiceAsync(this Telegram telegram, SendVoiceRequest sendRequest, string file = null)
        {
            var result = new Message();

            if (string.IsNullOrEmpty(file)
                && string.IsNullOrEmpty(sendRequest.Voice))
            {
                return null;
            }

            if (string.IsNullOrEmpty(sendRequest.Voice) && !string.IsNullOrEmpty(file) && Path.GetExtension(file) != ".ogg")
            {
                return null;
            }

            var url = telegram.GetFullPathUrl("sendVoice");

            using (var form = new MultipartFormDataContent())
            {
                AddParametersToRequest(form, sendRequest);
                using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    if (!string.IsNullOrEmpty(file))
                    {
                        form.Add(new StreamContent(fileStream), "voice", Path.GetFileName(file));
                    }

                    using (var client = new HttpClient())
                    {
                        var responseHendler = await client.PostAsync(url, form);
                        if (responseHendler != null)
                        {
                            var response = await responseHendler.Content.ReadAsStringAsync();
                            if (!string.IsNullOrEmpty(response))
                            {
                                result = Newtonsoft.Json.JsonConvert.DeserializeObject<ResponseAnswer<Message>>(response).Result;
                            }
                        }
                    }
                }
            }

            return result;
        }

        public static async Task<Message> SendLocationAsync(this Telegram telegram, SendLocationRequest sendLocationRequest)
        {
            var result = new Message();

            var url = telegram.GetFullPathUrl("sendLocation");

            var request = new ExternalRequest<ResponseAnswer<Message>, SendLocationRequest>()
            {
                Method = POST_METHOD,
                PostContentType = "application/json",
                PostContent = sendLocationRequest
            };

            var response = await RequestSender.Execute(DataAccessMode.Server, request, url).ConfigureAwait(false);

            result = response.Result.Result;

            return result;
        }

        public static async Task<Message> SendVenueAsync(this Telegram telegram, SendVenueRequest sendLocationRequest)
        {
            var result = new Message();

            var url = telegram.GetFullPathUrl("sendVenue");

            var request = new ExternalRequest<ResponseAnswer<Message>, SendVenueRequest>()
            {
                Method = POST_METHOD,
                PostContentType = "application/json",
                PostContent = sendLocationRequest
            };

            var response = await RequestSender.Execute(DataAccessMode.Server, request, url).ConfigureAwait(false);

            result = response.Result.Result;

            return result;
        }

        public static async Task<Message> SendContactAsync(this Telegram telegram, SendContactRequest sendRequest)
        {
            var result = new Message();

            var url = telegram.GetFullPathUrl("sendContact");

            var request = new ExternalRequest<ResponseAnswer<Message>, SendContactRequest>()
            {
                Method = POST_METHOD,
                PostContentType = "application/json",
                PostContent = sendRequest
            };

            var response = await RequestSender.Execute(DataAccessMode.Server, request, url).ConfigureAwait(false);

            result = response.Result.Result;

            return result;
        }

        public static async Task SendBotActionAsync(this Telegram telegram, SendBotActionRequest sendRequest)
        {
            var result = new Message();

            var url = telegram.GetFullPathUrl("sendChatAction");

            var request = new ExternalRequest<ResponseAnswer<Message>, SendBotActionRequest>()
            {
                Method = POST_METHOD,
                PostContentType = "application/json",
                PostContent = sendRequest
            };

            await RequestSender.Execute(DataAccessMode.Server, request, url).ConfigureAwait(false);
        }

        public static async Task<UserProfilePhotos> GetUserPrifilePhotoAsync(this Telegram telegram, GetUserProfilePhotoRequest sendRequest)
        {
            var result = new UserProfilePhotos();

            var url = telegram.GetFullPathUrl("getUserProfilePhotos");

            var request = new ExternalRequest<ResponseAnswer<UserProfilePhotos>, GetUserProfilePhotoRequest>()
            {
                Method = POST_METHOD,
                PostContentType = "application/json",
                PostContent = sendRequest
            };

            var response = await RequestSender.Execute(DataAccessMode.Server, request, url).ConfigureAwait(false);

            result = response.Result.Result;

            return result;
        }

        public static async Task<File> GetFileAsync(this Telegram telegram, GetFileRequest sendRequest)
        {
            var result = new File();

            var url = telegram.GetFullPathUrl("getFile");

            var request = new ExternalRequest<ResponseAnswer<File>, GetFileRequest>()
            {
                Method = POST_METHOD,
                PostContentType = "application/json",
                PostContent = sendRequest
            };

            var response = await RequestSender.Execute(DataAccessMode.Server, request, url).ConfigureAwait(false);

            result = response.Result.Result;

            return result;
        }

        public static async Task<bool> TryKickChatMemberAsync(this Telegram telegram, KickChatMemberRequest kickRequest)
        {
            var url = telegram.GetFullPathUrl("kickChatMember");

            var request = new ExternalRequest<ResponseAnswer<bool>, KickChatMemberRequest>()
            {
                Method = POST_METHOD,
                PostContentType = "application/json",
                PostContent = kickRequest
            };

            var response = await RequestSender.Execute(DataAccessMode.Server, request, url).ConfigureAwait(false);

            var isSuccess = response.Result.Result;

            return isSuccess;
        }

        public static async Task<bool> TryUnbanChatMemberAsync(this Telegram telegram, KickChatMemberRequest kickRequest)
        {
            var url = telegram.GetFullPathUrl("unbanChatMember");

            var request = new ExternalRequest<ResponseAnswer<bool>, KickChatMemberRequest>()
            {
                Method = POST_METHOD,
                PostContentType = "application/json",
                PostContent = kickRequest
            };

            var response = await RequestSender.Execute(DataAccessMode.Server, request, url).ConfigureAwait(false);

            var isSuccess = response.Result.Result;

            return isSuccess;
        }

        public static async Task<bool> SendCallbackAnswerAsync(this Telegram telegram, AnswerCallbackQueryRequest answer)
        {
            var url = telegram.GetFullPathUrl("answerCallbackQuery");

            var request = new ExternalRequest<ResponseAnswer<bool>, AnswerCallbackQueryRequest>()
            {
                Method = POST_METHOD,
                PostContentType = "application/json",
                PostContent = answer
            };

            var response = await RequestSender.Execute(DataAccessMode.Server, request, url).ConfigureAwait(false);

            var isSuccess = response.Result?.Result ?? false;

            return isSuccess;
        }

        public static async Task<Message> EditMessageAsync(this Telegram telegram, EditMessageTextRequest sendMessageRequest)
        {
            var result = new Message();

            var url = telegram.GetFullPathUrl("editMessageText");

            var request = new ExternalRequest<ResponseAnswer<Message>, EditMessageTextRequest>()
            {
                Method = POST_METHOD,
                PostContentType = "application/json",
                PostContent = sendMessageRequest
            };

            var response = await RequestSender.Execute(DataAccessMode.Server, request, url).ConfigureAwait(false);

            result = response.Result.Result;

            return result;
        }

        public static async Task<Message> EditMessageCaptionAsync(this Telegram telegram, EditMessageCationRequest sendMessageRequest)
        {
            var result = new Message();

            var url = telegram.GetFullPathUrl("editMessageText");

            var request = new ExternalRequest<ResponseAnswer<Message>, EditMessageCationRequest>()
            {
                Method = POST_METHOD,
                PostContentType = "application/json",
                PostContent = sendMessageRequest
            };

            var response = await RequestSender.Execute(DataAccessMode.Server, request, url).ConfigureAwait(false);

            result = response.Result.Result;

            return result;
        }

        public static async Task<Message> EditMessageReplyMarkupAsync(this Telegram telegram, EditReplyMarkupRequest sendMessageRequest)
        {
            var result = new Message();

            var url = telegram.GetFullPathUrl("editMessageText");

            var request = new ExternalRequest<ResponseAnswer<Message>, EditReplyMarkupRequest>()
            {
                Method = POST_METHOD,
                PostContentType = "application/json",
                PostContent = sendMessageRequest
            };

            var response = await RequestSender.Execute(DataAccessMode.Server, request, url).ConfigureAwait(false);

            result = response.Result.Result;

            return result;
        }

        

        private static void AddParametersToRequest<T>(MultipartFormDataContent content, T requestModel) where T : class
        {
            if (requestModel != null)
            {
                var type = requestModel.GetType();
                foreach (var propertyInfo in type.GetProperties())
                {
                    var value = propertyInfo.GetValue(requestModel);
                    if (value != null && !IsIgnoreableAttribute(propertyInfo))
                    {
                        if (!propertyInfo.GetType().IsPrimitive
                            && propertyInfo.PropertyType != typeof(string)
                            && propertyInfo.PropertyType != typeof(bool))
                        {
                            content.Add(new StringContent(JsonParser<object>.Serialize(value), Encoding.UTF8), GetAttributeDataMemberName(propertyInfo));
                        }
                        else
                        {
                            content.Add(new StringContent(value.ToString(), Encoding.UTF8), GetAttributeDataMemberName(propertyInfo));
                        }
                    }
                }
            }
        }

        private static string GetAttributeDataMemberName(PropertyInfo propertyInfo)
        {
            var result = string.Empty;

            var attributes = propertyInfo.GetCustomAttributes(true);
            if (attributes.Any())
            {
                foreach (var attr in attributes)
                {
                    if (attr is DataMemberAttribute serializeAttr)
                    {
                        result = serializeAttr.Name;
                        break;
                    }
                }
            }
            else
            {
                result = propertyInfo.Name;
            }

            return result;
        }

        private static bool IsIgnoreableAttribute(PropertyInfo propertyInfo)
        {
            var attributes = propertyInfo.GetCustomAttributes(true);
            if (attributes.Any())
            {
                foreach (var attr in attributes)
                {
                    if (attr is IgnoreDataMemberAttribute serializeAttr)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
