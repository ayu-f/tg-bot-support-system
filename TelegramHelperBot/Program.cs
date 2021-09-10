using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramHelperBot
{
    class Program
    {
        static string botToken;
        static string dbConnectionString;
        static DataBaseManager dbManager;
        static UserSessionsManager usManager;
        static HelperBot hBot;

        static void Main(string[] args)
        {
            try
            {
                //Чтение токена бота из файла botToken.txt
                StreamReader sr = new StreamReader("../../botToken.txt");
                botToken = sr.ReadLine();
                sr.Close();
                //Чтение строки подключения к БД из файла dbConnectionString.txt
                sr = new StreamReader("../../dbConnectionString.txt");
                dbConnectionString = sr.ReadLine();
                sr.Close();

                //Инициализация менеджеров БД и сессий
                dbManager = new DataBaseManager(dbConnectionString);
                usManager = new UserSessionsManager(dbManager);
                //Инициализация бота
                hBot = new HelperBot(botToken, usManager);

                //Запуск процедуры обработки приходящих обновлений
                hBot.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                if (ex.InnerException != null)
                {
                    Console.WriteLine(ex.InnerException.Message);
                }
            }
            Console.ReadKey();
            return;
        }
    }
}
