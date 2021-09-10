using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TelegramHelperBot
{
    class HelperBot
    {
        UserSessionsManager usManager;
        TelegramBotClient botClient;
        string botToken;

        public HelperBot(string botToken, UserSessionsManager usManager)
        {
            this.botToken = botToken;
            this.usManager = usManager;
            botClient = null;
        }

        public void Start()
        {
            try
            {
                //Запуск клиента и проверка корректности запуска
                botClient = new TelegramBotClient(botToken);
                User me = botClient.GetMeAsync().Result;
                if (me == null || string.IsNullOrEmpty(me.Username))
                {
                    throw new Exception("Invalid GetMe result.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Bot Start error:", ex);
            }

            try
            {
                //Номер ожидаемого апдейта
                int offset = 0;
                //Цикл обработки апдейтов
                while (true)
                {
                    //Проверка наличия новых апдейтов
                    Update[] nextUpdates = botClient.GetUpdatesAsync(offset).Result;
                    if (nextUpdates.Length > 0)
                    {
                        //Вызов обработки апдейтов менеджером сессий
                        usManager.ProcessNextUpdates(nextUpdates, offset);
                        //Получение массива запросов, появившихся после обработки, на изменение (отключение) inline кнопок в отправленных ранее сообщенях
                        var nextMarkupEditRequests = usManager.nextMarkupEditRequests;
                        foreach (var markupEdit in nextMarkupEditRequests)
                        {
                            //Запуск асинхронного изменения inline кнопок сообщения
                            botClient.EditMessageReplyMarkupAsync(markupEdit.chatId, markupEdit.inlineMessageId, markupEdit.replyMarkup);
                        }
                        //Получение массива запросов, появившихся после обработки, на отправку ответов
                        var nextReplytRequests = usManager.nextReplytRequests;
                        foreach (var reply in nextReplytRequests)
                        {
                            //Запуск задачи асинхронной отправки сообщения
                            botClient.SendTextMessageAsync(reply.chatId, reply.text, replyMarkup: reply.replyMarkup);
                        }
                        //Удаление неактивных сессий
                        usManager.RemoveOld();
                        //Изменение номера следующено ожидаемого апдейта
                        offset = nextUpdates.Last().Id + 1;
                    }
                    //Пауза, чтобы бот не был автоматически забанен
                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Bot Update error:", ex);
            }
        }
    }
}
