using System;
using System.Net;
using System.Web.Http;
using WebAPI.Auth;
using WebAPI.Data;
using WebAPI.Model;

namespace WebAPI.Controllers
{
    public class UserController : ApiController
    {
        ICustomerRepository rep;
        readonly ITokenService tokenService;

        /// <param name="rep">Reads and stores users.</param>
        /// <param name="tokenService">Issues bearer tokens on login.</param>
        /// <exception cref="ArgumentNullException">When either argument is null.</exception>
        public UserController(ICustomerRepository rep, ITokenService tokenService)
        {
            if (rep == null)
            {
                throw new ArgumentNullException("rep");
            }
            if (tokenService == null)
            {
                throw new ArgumentNullException("tokenService");
            }
            this.rep = rep;
            this.tokenService = tokenService;
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

        /// <summary>
        /// Registers a user.
        /// </summary>
        /// <returns>200 with the new user; 400 when the email or password is invalid; 409 when the email is taken.</returns>
        [AllowAnonymous]
        [HttpPost]
        public IHttpActionResult Save(User oUser)
        {
            if (oUser == null)
            {
                return BadRequest("Email and password are required.");
            }
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (rep.EmailExists(oUser.Email))
            {
                return Content(HttpStatusCode.Conflict, "An account with this email already exists.");
            }

            oUser.CreatedAt = DateTime.UtcNow;
            oUser.ModifiedAt = oUser.CreatedAt;
            var s = rep.Save(oUser);
            return Ok(s);
        }


        /// <summary>
        /// Checks the credentials and, when they match, issues a bearer token for the user.
        /// </summary>
        /// <param name="oUser">Email and password.</param>
        /// <returns>200 with a <see cref="LoginResponse"/>; 400 without credentials; 401 when they don't match.</returns>
        [AllowAnonymous]
        [HttpPost]
        public IHttpActionResult Login(User oUser)
        {
            if (oUser == null)
            {
                return BadRequest("Email and password are required.");
            }

            var user = rep.LogIn(oUser);

            if (user == null)
            {
                return Unauthorized();
            }

            var token = tokenService.Issue(user.Id);
            return Ok(new LoginResponse
            {
                Token = token.Value,
                ExpiresAt = token.ExpiresAtUtc,
                UserId = user.Id,
                Email = user.Email
            });
        }

    }
}
