using System;
using System.Data.SQLite;

namespace WebAPI.Data
{
    public class SqLiteBaseRepository
    {
        public static string DbFile
        {
            get
            {
                return AppDomain.CurrentDomain.BaseDirectory + ("/App_data/SimpleDb.sqlite");
            }
        }

        public static SQLiteConnection SimpleDbConnection()
        {

            return new SQLiteConnection("Data Source=" + DbFile);
        }
    }
}