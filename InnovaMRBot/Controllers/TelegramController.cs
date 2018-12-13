using System;
using InnovaMRBot.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
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

        //[HttpPost]
        //[Route("some")]
        //public void GetUpdateFromTelegram([FromBody]List<Update> updates)
        //{
        //    _telegram.SetupChanges(updates);
        //}

        [HttpPost]
        [Route("some")]
        public IActionResult GetUpdateFromTelegram([FromBody]string updateString)
        {
            Request.Body.Position = 0;
            StreamReader reader = new StreamReader(Request.Body);
            string text = reader.ReadToEnd();

            try
            {
                var updates = JsonConvert.DeserializeObject<List<Update>>(text);
                _telegram.SetupChanges(updates);
            }
            catch (Exception)
            {
                var update = JsonConvert.DeserializeObject<Update>(text);
                _telegram.SetupChanges(new List<Update>() { update });
            }

            return Json(updateString);
        }
    }
}
