using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace TelegramHelperBot
{
    class DataBaseManager
    {
        private MySqlConnection connection;

        public DataBaseManager(string dbConnectionString)
        {
            connection = new MySqlConnection(dbConnectionString);
        }
        private void OpenConnection()
        {
            if (connection.State != System.Data.ConnectionState.Open)
            {
                connection.Open();
            }
        }

        private void CloseConnection()
        {
            if (connection.State == System.Data.ConnectionState.Open)
            {
                connection.Close();
            }
        }

        public QuestionData GetQuestionData(int nodeId, string languageCode)
        {
            switch(languageCode)
            {
                case "ru":
                    languageCode = "RUS";
                    break;
                case "en":
                    languageCode = "ENG";
                    break;
                default:
                    languageCode = "RUS";
                    break;
            }
            QuestionData questionData = new QuestionData(nodeId);
            string nodeDataSqlCommandString =
                "SELECT `node`.`short_name`, `node_multiling_text`.`text` FROM `node` LEFT JOIN `node_multiling_text` ON `node_multiling_text`.`node_id` = `node`.`id` WHERE `node`.`id` = @nodeId AND `node_multiling_text`.`language` = @languageCode";
            MySqlCommand nodeDataSqlCommand = new MySqlCommand(nodeDataSqlCommandString, connection);
            string nodeLinksSqlCommandString = 
                "SELECT `node`.`id`, `article_links`.`short_name`, `article_links`.`link_text` " +
                "FROM `node` " +
                    "LEFT JOIN `article_links` ON `article_links`.`node_id` = `node`.`id` " +
                "WHERE `node`.`id` = @nodeId";
            MySqlCommand nodeLinksSqlCommand = new MySqlCommand(nodeLinksSqlCommandString, connection);
            string nodeOptionsSqlCommandString =
                "SELECT `node`.`id`, `node_option`.`short_name`, `option_multiling_text`.`text`, `node_option`.`next_node_id` " +
                "FROM `node` " +
                    "LEFT JOIN `node_option` ON `node_option`.`node_id` = `node`.`id` " +
	                "LEFT JOIN `option_multiling_text` ON `option_multiling_text`.`option_id` = `node_option`.`id` " +
                "WHERE `node`.`id` = @nodeId AND `option_multiling_text`.`language` = @languageCode";
            MySqlCommand nodeOptionsSqlCommand = new MySqlCommand(nodeOptionsSqlCommandString, connection);
            MySqlDataReader dataReader;
            try
            {
                nodeDataSqlCommand.Parameters.AddWithValue("@nodeId", nodeId);
                nodeDataSqlCommand.Parameters.AddWithValue("@languageCode", languageCode);
                OpenConnection();
                dataReader = nodeDataSqlCommand.ExecuteReader();
                if (!dataReader.HasRows)
                {
                    throw new Exception("Node not found.");
                }
                dataReader.Read();
                questionData.shortName = dataReader.GetString("short_name");
                questionData.nodeText = dataReader.GetTextReader(dataReader.GetOrdinal("text")).ReadToEnd();
                CloseConnection();

                nodeLinksSqlCommand.Parameters.AddWithValue("@nodeId", nodeId);
                nodeLinksSqlCommand.Parameters.AddWithValue("@languageCode", languageCode);
                OpenConnection();
                dataReader = nodeLinksSqlCommand.ExecuteReader();
                while (dataReader.Read())
                {
                    questionData.linkList.Add(dataReader.GetTextReader(dataReader.GetOrdinal("link_text")).ReadToEnd());
                }
                CloseConnection();

                nodeOptionsSqlCommand.Parameters.AddWithValue("@nodeId", nodeId);
                nodeOptionsSqlCommand.Parameters.AddWithValue("@languageCode", languageCode);
                OpenConnection();
                dataReader = nodeOptionsSqlCommand.ExecuteReader();
                while (dataReader.Read())
                {
                    Option option = new Option
                    {
                        shortName = dataReader.GetString("short_name"),
                        text = dataReader.GetTextReader(dataReader.GetOrdinal("text")).ReadToEnd(),
                        nextNodeId = dataReader.GetInt32("next_node_id")
                    };
                    questionData.optionList.Add(option);
                }
                CloseConnection();
            }
            catch (Exception ex)
            {
                if (ex.Message == "Node not found.")
                {
                    questionData = null;
                }
                else
                {
                    throw ex;
                }
            }
            finally
            {
                CloseConnection();
            }

            return questionData;
        }
    }
}
