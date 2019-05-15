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
        public IHttpActionResult GetByUserId(int UserId)
        {
            var fuelDetails = rep.GetListFuelDetailByUserId(new FuelDetail() { UserId = UserId });
            if (fuelDetails == null)
            {
                return NotFound();
            }

            return Ok(fuelDetails);
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

        [HttpGet]
        public IHttpActionResult GetFuelDetailById(int Id)
        {
            var fuelDetails = rep.GetFuelDetailById(new FuelDetail() { Id = Id });

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


    }
}
