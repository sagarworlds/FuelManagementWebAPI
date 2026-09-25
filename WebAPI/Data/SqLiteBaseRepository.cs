using System;
using System.Data.SQLite;
using WebAPI.Model;

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

            // DateTimeKind=Utc: DateTimes are written with a "Z" and read back as UTC. Without it,
            // stored "Z" values are converted to the server's local time and returned without an offset.
            return new SQLiteConnection("Data Source=" + DbFile + ";DateTimeKind=Utc");
        }
    }
}