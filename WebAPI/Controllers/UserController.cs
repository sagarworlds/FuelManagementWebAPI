using System.Web.Http;
using WebAPI.Data;
using WebAPI.Model;

namespace WebAPI.Controllers
{
    public class UserController : ApiController
    {
        ICustomerRepository rep;
        public UserController()
        {
            rep = new SqLiteCustomerRepository();
        }


        [HttpGet]
        public IHttpActionResult Get()
        {
            var users = rep.GetUser();
            if (users == null)
            {
                return NotFound();
            }

            return Ok(users);
        }

        [HttpPost]
        public IHttpActionResult Save(User oUser)
        {
            //rep.SaveUser(new User { Email = "sagarp@gmail.com", Password = "password", CreatedAt = DateTime.Now, ModifiedAt = DateTime.Now });
            rep.Save(oUser);
            var users = rep.GetUser();
            if (users == null)
            {
                return NotFound();
            }

            return Ok(users);
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
