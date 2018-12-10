using InnovaMRBot.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using TelegramBotApi.Models;
using TelegramBotApi.Telegram;

namespace InnovaMRBot.Controllers
{
    [Route("")]
    public class TelegramController : Controller
    {
        private Telegram _telegram;

        public TelegramController(Telegram telegram, ChatStateService chatService)
        {
            _telegram = telegram;
        }
        
        [HttpPost]
        [Route("some")]
        public void GetUpdateFromTelegram([FromBody]List<Update> updates)
        {
            _telegram.SetupChanges(updates);
        }
    }
}
