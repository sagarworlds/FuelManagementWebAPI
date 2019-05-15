using DAL.Model;
using Dapper;
using System.Data.SQLite;
using System.Linq;


namespace DAL
{
    public class SettingContext
    {
        private readonly string _connectionString;

        public SettingContext()
        {
            _connectionString = Utilities.GetConnectionString();
        }
        public Setting GetSettingByCustomerId(int CustomerId)
        {
            using (var _connection = new SQLiteConnection(_connectionString))
            {
                _connection.Open();
                var result = _connection.Query<Setting>("SELECT * FROM tbl_Setting WHERE CustomerId=@CustomerId", new { CustomerId = CustomerId }).FirstOrDefault();
                return result;
            }
        }

    }
}
