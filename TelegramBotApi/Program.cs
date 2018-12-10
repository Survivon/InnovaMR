using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramBotApi
{
    using Extension;
    using Telegram.Events;
    using Telegram.Request;

    class Program
    {
        static void Main(string[] args)
        {
            var chatID = 360306617;

            var telegram = new Telegram.Telegram();

            telegram.OnUpdateReceive +=TelegramOnUpdateReceive;

            Task.Run(async () =>
                     {

                         //await telegram.SendPhotoAsync(new SendPhotoRequest() { ChatId = chatID.ToString(), Caption = "122211"}, "C:\\Users\\ereme\\Pictures\\007.jpg");
                         //await telegram.SendMessageAsync(new SendMessageRequest() { ChatId = chatID.ToString(), Text = "1112233" });

                         //await telegram.SendAudioAsync(new SendAudioRequest() { ChatId = chatID.ToString(), Caption = "122211" }, @"C:\Users\ereme\Downloads\Lui Armstrong – What a wonderful world.mp3");

                         //await telegram.SendDocumentAsync(new SendDocumentRequest() {ChatId = chatID.ToString(), Caption = "122211"}, @"C:\Users\ereme\Downloads\Red-Alert-3.torrent");
                         await telegram.SendStickerAsync(new SendStickerRequest() {ChatId = chatID.ToString()}, @"C:\Users\ereme\Downloads\Red-Alert-3.torrent");
                     });


            Console.ReadKey();
        }

        private static void TelegramOnUpdateReceive(object sender, UpdateEventArgs updateEventArgs)
        {
            Console.WriteLine(updateEventArgs.Updates.FirstOrDefault().Message.Text);
            Console.WriteLine(updateEventArgs.Updates.FirstOrDefault().Id);
        }
    }
}
