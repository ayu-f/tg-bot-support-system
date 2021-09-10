using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramHelperBot
{
    class UserSession
    {
        public long chatId;
        public int lastUpdateOffset;
        public Stack<int> questionsIds;
        const int beginOfNode = 4;
        public QuestionData currentQuestion = null;
        public DateTime lastUpdateTime;
        public UserSession(long chatId)
        {
            this.chatId = chatId;
            questionsIds = new Stack<int>();
            lastUpdateOffset = -1;
        }

        private ReplytRequest PrepareReplytRequest() 
        {
            string finalText = "";

            foreach (var link in currentQuestion.linkList)
            {
                finalText = string.Concat(finalText, "\n", link);
            }
            finalText = string.Concat(currentQuestion.nodeText, "\n", finalText);

            if(questionsIds.Count == 0)
            {
                return new ReplytRequest(chatId, finalText, currentQuestion.optionList);
            }
            else
            {
                return new ReplytRequest(chatId, finalText, currentQuestion.optionList, currentQuestion.nodeId);
            }
        }

        private void ProcessingCallbackQuery(DataBaseManager dbManager, Update upd)
        {
            if(upd.CallbackQuery.Data == ReplytRequest.backButton.shortName + currentQuestion.nodeId.ToString())
            {
                if (questionsIds.Count == 0)
                {
                    return;
                }
                else
                {
                    // предыдущий вопрос
                    QuestionData prevQuestion = dbManager.GetQuestionData(questionsIds.Pop(), upd.CallbackQuery.From.LanguageCode);
                    if (prevQuestion == null)
                        throw new Exception("Prev question DataBase error");
                    currentQuestion = prevQuestion;
                    return;
                }
            }

            Option foundOption = null;
            foreach (var option in currentQuestion.optionList)
            {
                if (upd.CallbackQuery.Data == option.shortName)
                {
                    foundOption = option;
                    break;
                }
            }
            if (foundOption == null)
            {
                throw new Exception("Invalid option");
            }
            questionsIds.Push(currentQuestion.nodeId);
            QuestionData newQuestion = dbManager.GetQuestionData(foundOption.nextNodeId, upd.CallbackQuery.From.LanguageCode);
            if (newQuestion == null)
            {
                throw new Exception("DataBase error");
            }
            currentQuestion = newQuestion;
        }

        public void ProcessUpdate(DataBaseManager dbManager, long chatId, Update upd, int offset, out ReplytRequest replytRequest, out DeleteKeyboardRequest editRequest)
        {
            lastUpdateTime = DateTime.Now;
            lastUpdateOffset = offset;
            editRequest = null;
            replytRequest = null;
            switch (upd.Type)
            {
                case UpdateType.CallbackQuery:
                    if (currentQuestion == null)
                    {
                        questionsIds.Clear();
                        currentQuestion = dbManager.GetQuestionData(beginOfNode, upd.CallbackQuery.From.LanguageCode);
                        replytRequest = PrepareReplytRequest();
                        break;
                    }
                    editRequest = new DeleteKeyboardRequest(chatId, upd.CallbackQuery.Message.MessageId);
                    try
                    {
                        ProcessingCallbackQuery(dbManager, upd);
                        replytRequest = PrepareReplytRequest();
                    }
                    catch(Exception ex)
                    {
                        if(ex.Message == "Invalid option")
                        {
                            replytRequest = null;
                            editRequest = null;
                        }
                        else
                        {
                            throw ex;
                        }
                    }
                    
                    break;
                case UpdateType.Message:
                    if (currentQuestion == null || upd.Message.Text == "/start")
                    {
                        questionsIds.Clear();
                        currentQuestion = dbManager.GetQuestionData(beginOfNode, upd.Message.From.LanguageCode);
                        replytRequest = PrepareReplytRequest();
                    }
                    break;
                default:
                    break;
            }
        }
    }
}
