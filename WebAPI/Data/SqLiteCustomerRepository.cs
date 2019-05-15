using System.IO;
using System.Linq;
using Dapper;
using WebAPI.Model;

namespace WebAPI.Data
{
    public class SqLiteCustomerRepository : SqLiteBaseRepository, ICustomerRepository
    {

        public User[] GetUser()
        {
            using (var cnn = SimpleDbConnection())
            {
                cnn.Open();
                User[] result = cnn.Query<User>(
                    @"SELECT Id, Email, Password, CreatedAt, ModifiedAt
                    FROM User").ToArray();
                return result;
            }
        }


        public void Save(User oUser)
        {
            using (var cnn = SimpleDbConnection())
            {
                cnn.Open();
                oUser.Id = cnn.Query<int>(
                    @"INSERT INTO User 
                    ( Email, Password, CreatedAt, ModifiedAt  ) VALUES 
                    ( @Email, @Password, @CreatedAt, @ModifiedAt );
                    select last_insert_rowid()", oUser).First();
            }
        }
        
        public void Save(FuelDetail oFuelDetails)
        {
            using (var cnn = SimpleDbConnection())
            {
                cnn.Open();
                oFuelDetails.Id = cnn.Query<int>(
                    @"INSERT INTO FuelDetail
                    ( UserId, MeterReading, AddedFuel, Note, CreatedAt, ModifiedAt  ) VALUES 
                    ( @UserId, @MeterReading, @AddedFuel, @Note, @CreatedAt, @ModifiedAt );
                    select last_insert_rowid()", oFuelDetails).First();
            }
        }
        
        public User LogIn(User oUser)
        {
            using (var cnn = SimpleDbConnection())
            {
                cnn.Open();
                //var result = cnn.Query<User>("SELECT Id, Email, Password, CreatedAt, ModifiedAt FROM User WHERE Email=@email AND Password=@password", new { email = oUser.Email, password = oUser.Password }).FirstOrDefault();
                var result = cnn.Query<User>("SELECT * FROM User WHERE Email=@email AND Password=@password", new { email = oUser.Email, password = oUser.Password }).FirstOrDefault();
                return result;
            }
        }

        public FuelDetail[] GetListFuelDetail()
        {
            using (var cnn = SimpleDbConnection())
            {
                cnn.Open();
                FuelDetail[] result = cnn.Query<FuelDetail>(
                    @"SELECT * FROM FuelDetail").ToArray();
                return result;
            }
        }

    }
}
