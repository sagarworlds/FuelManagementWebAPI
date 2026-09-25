using System;
using WebAPI.Model;

namespace WebAPI.Data
{
    public interface ICustomerRepository
    {
        User[] GetUser();
        FuelDetail[] GetListFuelDetail();
        FuelDetail[] GetListFuelDetailByUserId(FuelDetail oFuelDetail);
        
        FuelDetail GetFuelDetailById(FuelDetail oFuelDetail);
        FuelDetail Save(FuelDetail oFuelDetails);
        User Save(User oUser);

        /// <summary>The user with this email, ignoring letter case, or null.</summary>
        User GetUserByEmail(string email);

        /// <summary>The user with this id, or null.</summary>
        User GetUserById(int id);

        /// <summary>Replaces a user's stored password hash.</summary>
        void UpdatePassword(int userId, string passwordHash, DateTime modifiedAt);

        /// <summary>Whether a user with this email exists, ignoring letter case.</summary>
        bool EmailExists(string email);
    }
}