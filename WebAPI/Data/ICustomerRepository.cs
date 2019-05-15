using WebAPI.Model;

namespace WebAPI.Data
{
    public interface ICustomerRepository
    {
        User[] GetUser();
        FuelDetail[] GetListFuelDetail();
        FuelDetail GetFuelDetailById(FuelDetail oFuelDetail);
        FuelDetail Save(FuelDetail oFuelDetails);
        User Save(User oUser);
        User LogIn(User oUser);
    }
}