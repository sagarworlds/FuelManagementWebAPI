using Dapper;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL
{
    public class Utilities
    {
        private static readonly string ConnectionString = ConfigurationManager.AppSettings.Get("DBconnection");

        public static string GetConnectionString()
        {

            var dbFile = AppDomain.CurrentDomain.BaseDirectory + (ConnectionString);
            var strConnection = string.Format(@"Data Source={0}; Pooling=false; FailIfMissing=false;", dbFile);

            return strConnection;
        }
    }
}
