using Dapper;
using System.Collections.Generic;
using System.Data.SQLite;

namespace DAL
{

    public class DBContext<T>
    {
        private readonly string _connectionString;

        public DBContext()
        {
            _connectionString = Utilities.GetConnectionString();
        }
        #region Generic Methods

        public IEnumerable<T> GetList()
        {
            using (var _connection = new SQLiteConnection(_connectionString))
            {
                _connection.Open();
                return SimpleCRUD.GetList<T>(_connection);
            }
        }

        public T Get(int id)
        {
            using (var _connection = new SQLiteConnection(_connectionString))
            {
                _connection.Open();
                return SimpleCRUD.Get<T>(_connection, id);
            }
        }

        public IdSucess Add(T t)
        {
            using (var _connection = new SQLiteConnection(_connectionString))
            {
                _connection.Open();
                var result = SimpleCRUD.Insert(_connection, t);
                return new IdSucess{ Id = result, IsSuccess = result > 0 ? true : false };
            }
        }

        public bool Update(T t)
        {
            using (var _connection = new SQLiteConnection(_connectionString))
            {
                _connection.Open();
                var result = SimpleCRUD.Update(_connection, t);
                return result > 0 ? true : false;
            }
        }

        public bool Delete(T t)
        {
            using (var _connection = new SQLiteConnection(_connectionString))
            {
                _connection.Open();
                var result = SimpleCRUD.Delete(_connection, t);
                return result > 0 ? true : false;
            }
        }

        #endregion


        public int RecordCount()
        {
            using (var _connection = new SQLiteConnection(_connectionString))
            {
                _connection.Open();
                var result = SimpleCRUD.RecordCount<T>(_connection);
                return result;
            }
        }
              
        
    }

    public class IdSucess {
        public int? Id { get; set; }
        public bool IsSuccess { get; set; }
    }
}
