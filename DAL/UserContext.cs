using DAL.Model;
using DAL.Models;
using Dapper;
using System.Data.SQLite;
using System.Linq;

namespace DAL
{
    public class UserContext : DBContext<AUser>
    {
        private readonly string _connectionString;
                
        public UserContext()
        {
            _connectionString = Utilities.GetConnectionString();
        }

        public AUser LogIn(AUser user)
        {
            using (var _connection = new SQLiteConnection(_connectionString))
            {
                _connection.Open();
                //var result = _connection.Query <AUser>("SELECT * FROM tbl_User WHERE Email=@email AND Password=@password", new { email = user.Email, password = user.Password }).FirstOrDefault();
                var result = _connection.Query("SELECT * FROM tbl_User WHERE Email=@email AND Password=@password", new { email = user.Email, password = user.Password }).FirstOrDefault();
                return result;
            }

        }
    }
}
