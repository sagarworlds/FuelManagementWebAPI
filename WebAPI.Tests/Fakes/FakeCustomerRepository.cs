using System;
using System.Collections.Generic;
using System.Linq;
using WebAPI.Data;
using WebAPI.Model;

namespace WebAPI.Tests.Fakes
{
    /// <summary>
    /// In-memory stand-in for the SQLite repository.
    /// </summary>
    internal class FakeCustomerRepository : ICustomerRepository
    {
        public readonly List<User> Users = new List<User>();
        public readonly List<FuelDetail> FuelDetails = new List<FuelDetail>();

        public User[] GetUser()
        {
            return Users.ToArray();
        }

        public FuelDetail[] GetListFuelDetail()
        {
            return FuelDetails.ToArray();
        }

        public FuelDetail[] GetListFuelDetailByUserId(FuelDetail oFuelDetail)
        {
            return FuelDetails.Where(f => f.UserId == oFuelDetail.UserId).ToArray();
        }

        public FuelDetail GetFuelDetailById(FuelDetail oFuelDetail)
        {
            return FuelDetails.FirstOrDefault(f => f.Id == oFuelDetail.Id);
        }

        public FuelDetail Save(FuelDetail oFuelDetails)
        {
            oFuelDetails.Id = FuelDetails.Count == 0 ? 1 : FuelDetails.Max(f => f.Id) + 1;
            FuelDetails.Add(oFuelDetails);
            return oFuelDetails;
        }

        public User Save(User oUser)
        {
            oUser.Id = Users.Count == 0 ? 1 : Users.Max(u => u.Id) + 1;
            Users.Add(oUser);
            return oUser;
        }

        public User GetUserByEmail(string email)
        {
            return Users.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
        }

        public User GetUserById(int id)
        {
            return Users.FirstOrDefault(u => u.Id == id);
        }

        public void UpdatePassword(int userId, string passwordHash, DateTime modifiedAt)
        {
            var user = Users.Single(u => u.Id == userId);
            user.Password = passwordHash;
            user.ModifiedAt = modifiedAt;
        }

        public bool EmailExists(string email)
        {
            return Users.Any(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
        }
    }
}
