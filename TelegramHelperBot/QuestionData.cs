using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramHelperBot
{
    public class Option
    {
        public string shortName;
        public string text;
        public int nextNodeId;
    }
    class QuestionData
    {
        public int nodeId;
        public string shortName;
        public string nodeText;
        public List<string> linkList;
        public List<Option> optionList;

        public QuestionData(int nodeId)
        {
            this.nodeId = nodeId;
            linkList = new List<string>();
            optionList = new List<Option>();
        }
    }
}
