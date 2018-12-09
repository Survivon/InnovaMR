namespace TelegramBotApi.Telegram.DataAccess.Request
{
    using System;
    using System.Collections;
    using System.Threading.Tasks;
    using Requests;

    internal static class RequestSender
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public static async Task<Response<T>> Execute<T>(DataAccessMode mode, Request<T> request, string link)
        {
            var cacheApiResult = new Response<T>();
            
            if (mode != DataAccessMode.Cache)
            {
                if (cacheApiResult.Result == null)
                {
                    var serverResult = await request.ExecuteAsync(link);
                    if (!serverResult.IsSuccess)
                    {
                        bool hasCachedResult = false;
                        if (mode != DataAccessMode.Server)
                        {
                            // Return error only if we have no data for displaying.
                            hasCachedResult = !Object.Equals(cacheApiResult.Result, default(T));
                            if (hasCachedResult)
                            {
                                bool isCollection = cacheApiResult.Result is ICollection;
                                if (isCollection)
                                {
                                    var collection = (ICollection)cacheApiResult.Result;
                                    hasCachedResult = collection.Count > 0;
                                }
                            }
                        }

                        if (!hasCachedResult)
                        {
                            cacheApiResult = serverResult;
                            cacheApiResult.ResultSource = DataAccessMode.Server;
                        }
                    }
                    else
                    {
                        cacheApiResult.Result = serverResult.Result;
                        cacheApiResult.ResultSource = DataAccessMode.Server;
                    }
                }
            }

            return cacheApiResult;
        }
    }
}
