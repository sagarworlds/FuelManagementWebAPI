using System;
using System.Net;
using System.Web.Http;
using WebAPI.Auth;
using WebAPI.Data;
using WebAPI.Model;

namespace WebAPI.Controllers
{
    public class FuelDetailController : ApiController
    {
        ICustomerRepository rep;

        /// <param name="rep">Reads and stores fuel entries.</param>
        /// <exception cref="ArgumentNullException">When <paramref name="rep"/> is null.</exception>
        public FuelDetailController(ICustomerRepository rep)
        {
            if (rep == null)
            {
                throw new ArgumentNullException("rep");
            }
            this.rep = rep;
        }

        /// <summary>
        /// Returns a user's entries; only the signed-in user's own id is allowed.
        /// </summary>
        /// <returns>200 with the entries, or 403 for another user's id.</returns>
        [HttpGet]
        public IHttpActionResult GetByUserId(int UserId)
        {
            if (UserId != User.GetUserId())
            {
                return StatusCode(HttpStatusCode.Forbidden);
            }

            var fuelDetails = rep.GetListFuelDetailByUserId(new FuelDetail() { UserId = UserId });
            if (fuelDetails == null)
            {
                return NotFound();
            }

            return Ok(fuelDetails);
        }


        /// <summary>
        /// Returns the signed-in user's entries.
        /// </summary>
        [HttpGet]
        public IHttpActionResult Get()
        {
            var fuelDetails = rep.GetListFuelDetailByUserId(new FuelDetail() { UserId = User.GetUserId() });
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

            // Another user's entry is reported as missing rather than forbidden, so ids can't be probed.
            if (fuelDetails == null || fuelDetails.UserId != User.GetUserId())
            {
                return NotFound();
            }

            return Ok(fuelDetails);
        }



        /// <summary>
        /// Stores an entry for the signed-in user.
        /// </summary>
        /// <returns>200 with the stored entry, or 400 without a body or with invalid values.</returns>
        [HttpPost]
        public IHttpActionResult Save(FuelDetail oFuelDetail)
        {
            if (oFuelDetail == null)
            {
                return BadRequest("A fuel detail is required.");
            }
            // Also catches values the JSON couldn't be read into (e.g. text for a number),
            // which would otherwise be stored as 0.
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // The owner comes from the bearer token, never from the request body.
            oFuelDetail.UserId = User.GetUserId();
            oFuelDetail.ModifiedAt = DateTime.UtcNow;
            var ofuelDetail = rep.Save(oFuelDetail);
            return Ok(ofuelDetail);
        }


    }
}
