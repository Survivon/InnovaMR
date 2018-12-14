using InnovaMRBot.Services;
using Microsoft.AspNetCore.Mvc;
using TelegramBotApi.Models;

namespace InnovaMRBot.Controllers
{
    [Route("")]
    public class TelegramController : Controller
    {
        private readonly ChatStateService _chatService;

        public TelegramController(ChatStateService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost]
        [Route("some")]
        public void GetUpdateFromTelegramAsync([FromBody]Update update)
        {
            if (update == null) return;

            _chatService.GetUpdateFromTelegramAsync(update).ConfigureAwait(false);
        }
    }
}
