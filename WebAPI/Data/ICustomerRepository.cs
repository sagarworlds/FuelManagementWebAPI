using WebAPI.Model;

namespace WebAPI.Data
{
    public interface ICustomerRepository
    {
        User[] GetUser();
        FuelDetail[] GetListFuelDetail();
        void Save(User oUser);
        void Save(FuelDetail oFuelDetails);
        User LogIn(User oUser);
    }
}