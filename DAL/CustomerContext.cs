using DAL.Model;
using Dapper;
using System.Data.SQLite;
using System.Linq;


namespace DAL
{
    public class CustomerContext : DBContext<Customer>
    {
        private readonly string _connectionString;

        public CustomerContext()
        {
            _connectionString = Utilities.GetConnectionString();
        }
        public Customer GetByAPiKey(string apikey)
        {
            using (var _connection = new SQLiteConnection(_connectionString))
            {
                _connection.Open();
                var result = _connection.Query<Customer>("SELECT * FROM tbl_Customer WHERE ApiKey=@apikey", new { ApiKey = apikey }).FirstOrDefault();
                return result;
            }
        }

      
    }
}
