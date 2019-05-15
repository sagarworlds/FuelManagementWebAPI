using System;
using System.Web.Http;
using WebAPI.Data;
using WebAPI.Model;

namespace WebAPI.Controllers
{
    public class FuelDetailController : ApiController
    {
        ICustomerRepository rep;
        public FuelDetailController()
        {
            rep = new SqLiteCustomerRepository();
        }

        [HttpGet]
        public IHttpActionResult Get()
        {
            var fuelDetails = rep.GetListFuelDetail();
            if (fuelDetails == null)
            {
                return NotFound();
            }

            return Ok(fuelDetails);
        }
        

        [HttpPost]
        public IHttpActionResult Save(FuelDetail oFuelDetail)
        {
            oFuelDetail.CreatedAt = DateTime.UtcNow;
            oFuelDetail.ModifiedAt = DateTime.UtcNow;
            var ofuelDetail = rep.Save(oFuelDetail);            
            return Ok(ofuelDetail);
        }

        [HttpPost]
        public IHttpActionResult SaveUser(User oUser)
        {
            oUser.CreatedAt = DateTime.UtcNow;
            oUser.ModifiedAt = DateTime.UtcNow;
            var ofuelDetail = rep.Save(oUser);
            return Ok(ofuelDetail);
        }


        [HttpPost]
        public IHttpActionResult Login(User oUser)
        {
            var user = rep.LogIn(oUser);

            if (user == null)
            {
                return NotFound();
            }
            return Ok(user);
        }

    }
}
