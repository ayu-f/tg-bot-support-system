using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramHelperBot
{
    class UserSessionsManager
    {
        DataBaseManager dbManager;
        Dictionary<long, UserSession> activeSessions;
        

        public UserSessionsManager(DataBaseManager dbManager)
        {
            this.dbManager = dbManager;
            activeSessions = new Dictionary<long, UserSession>(50);
            nextMarkupEditRequests = new List<DeleteKeyboardRequest>();
            nextReplytRequests = new List<ReplytRequest>();
            
        }

        //Массив запросов на редактирование предыдущих сообщений (в часности отключение inline кнопок)
        public List<DeleteKeyboardRequest> nextMarkupEditRequests;

        //Массив запросов на отправку сообщений
        public List<ReplytRequest> nextReplytRequests;

        static long GetChatIdOfUpdate(Update update)
        {
            switch (update.Type)
            {
                case UpdateType.Message:
                    return update.Message.Chat.Id;
                case UpdateType.CallbackQuery:
                    return update.CallbackQuery.Message.Chat.Id;
                default:
                    throw new Exception("Not supported update type.");
            }
        }

        //Обрабатывает ответы в чатах из activeSessions, если первое сообщение, то создает новую сессию
        //Если от одного пользователя было получено более одного сообщения, то обрабатывается только первое
        //Для этого в каждой сессии хранится оффсет - id первого апдейта последней обработанной им группы
        public void ProcessNextUpdates(Update[] nextUpdates, int offset)
        {
            nextReplytRequests.Clear();
            nextMarkupEditRequests.Clear();
            foreach (var upd in nextUpdates)
            {
                long chatId;
                try
                {
                    chatId = GetChatIdOfUpdate(upd);
                }
                catch (Exception)
                {
                    continue;
                }

                UserSession session;
                if (activeSessions.ContainsKey(chatId))
                {
                    session = activeSessions[chatId];
                }
                else
                {
                    session = new UserSession(chatId);
                    activeSessions.Add(chatId, session);
                }

                if (session.lastUpdateOffset != offset)
                {
                    session.ProcessUpdate(dbManager, chatId, upd, offset, out ReplytRequest replytRequest, out DeleteKeyboardRequest editRequest);
                    if (replytRequest != null) nextReplytRequests.Add(replytRequest);
                    if (editRequest != null) nextMarkupEditRequests.Add(editRequest);
                }
                else
                {
                    continue;
                }
            }
        }

        //Удаляет из activeSessions сессии, которые не были активны долгое время
        public void RemoveOld()
        {
            if (activeSessions.Count > 100)
            {
                List< KeyValuePair<long, UserSession>> oldList = activeSessions.Where( pair => DateTime.Now.Subtract(pair.Value.lastUpdateTime).Days > 30).ToList();
                foreach (var item in oldList)
                {
                    activeSessions.Remove(item.Key);
                }
            }
        }
    }

    //Хранит данные, которые нужно передать боту для отправки ответа
    public class ReplytRequest
    {
        public readonly long chatId;
        public readonly string text;
        public readonly InlineKeyboardMarkup replyMarkup;
        public static Option backButton = new Option { shortName = "Back", text = "Назад", nextNodeId = 0 };
        
        public ReplytRequest(long chatId, string text, List<Option> options, int backButtonId = -1)
        {
            this.chatId = chatId;
            this.text = text;
            // лист листов для печати каждой inline кнопки с новой строки
            List<List<InlineKeyboardButton>> keyboard = new List<List<InlineKeyboardButton>>(options.Count);
            if (backButtonId != -1)// добавить кнопку назад
            {
                List<InlineKeyboardButton> back = new List<InlineKeyboardButton>();
                back.Add(new InlineKeyboardButton { CallbackData = backButton.shortName + backButtonId.ToString(), Text = backButton.text });
                keyboard.Add(back);
            }
            foreach (var opt in options)
            {
                List<InlineKeyboardButton> keyboardRow = new List<InlineKeyboardButton>();
                var button = new InlineKeyboardButton
                {
                    CallbackData = opt.shortName,
                    Text = opt.text
                };
                keyboardRow.Add(button);
                keyboard.Add(keyboardRow);
            }
            replyMarkup = new InlineKeyboardMarkup(keyboard);
        }
    }

    //Хранит данные, которые нужно передать боту для изменения (отключения) inline кнопок в отправленном ранее сообщении
    public class DeleteKeyboardRequest
    {
        public readonly long chatId;
        public readonly int inlineMessageId;
        public readonly InlineKeyboardMarkup replyMarkup;

        public DeleteKeyboardRequest(long chatId, int inlineMessageId)
        {
            this.chatId = chatId;
            this.inlineMessageId = inlineMessageId;
            replyMarkup = null;
        }
    }
}
