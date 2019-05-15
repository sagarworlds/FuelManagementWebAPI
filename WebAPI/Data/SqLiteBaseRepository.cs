using System;
using System.Data.SQLite;
using WebAPI.Controllers;

namespace WebAPI.Data
{
    public class SqLiteBaseRepository
    {
        public static string DbFile
        {
            get
            {
                return AppDomain.CurrentDomain.BaseDirectory + (AppSettings.DbConnection);
            }
        }

        public static SQLiteConnection SimpleDbConnection()
        {

            return new SQLiteConnection("Data Source=" + DbFile);
        }
    }
}