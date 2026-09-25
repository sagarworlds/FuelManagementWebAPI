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
        readonly IPasswordHasher passwordHasher;

        /// <param name="rep">Reads and stores users.</param>
        /// <param name="tokenService">Issues bearer tokens on login.</param>
        /// <param name="passwordHasher">Hashes and checks passwords.</param>
        /// <exception cref="ArgumentNullException">When any argument is null.</exception>
        public UserController(ICustomerRepository rep, ITokenService tokenService, IPasswordHasher passwordHasher)
        {
            if (rep == null)
            {
                throw new ArgumentNullException("rep");
            }
            if (tokenService == null)
            {
                throw new ArgumentNullException("tokenService");
            }
            if (passwordHasher == null)
            {
                throw new ArgumentNullException("passwordHasher");
            }
            this.rep = rep;
            this.tokenService = tokenService;
            this.passwordHasher = passwordHasher;
        }

        /// <summary>
        /// Registers a user; the password is stored as a bcrypt hash.
        /// </summary>
        /// <returns>200 with the new user (without password); 400 when the email or password is invalid; 409 when the email is taken.</returns>
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

            oUser.Password = passwordHasher.Hash(oUser.Password);
            oUser.CreatedAt = DateTime.UtcNow;
            oUser.ModifiedAt = oUser.CreatedAt;
            var s = rep.Save(oUser);
            return Ok(UserResponse.From(s));
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

            var user = string.IsNullOrEmpty(oUser.Email) ? null : rep.GetUserByEmail(oUser.Email);

            // Checked even when the email is unknown (against a dummy hash), so both cases take as long.
            if (!passwordHasher.Verify(oUser.Password, user == null ? null : user.Password))
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

        /// <summary>
        /// Changes the signed-in user's password. Tokens issued earlier stay valid until they expire.
        /// </summary>
        /// <returns>
        /// 204 on success; 400 when the new password is invalid or the current one is wrong (not 401,
        /// which clients treat as an expired session).
        /// </returns>
        [HttpPost]
        public IHttpActionResult ChangePassword(ChangePasswordRequest request)
        {
            if (request == null)
            {
                return BadRequest("CurrentPassword and NewPassword are required.");
            }
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = rep.GetUserById(User.GetUserId());
            if (user == null)
            {
                // The token belongs to a user that no longer exists.
                return Unauthorized();
            }
            if (!passwordHasher.Verify(request.CurrentPassword, user.Password))
            {
                return BadRequest("The current password is incorrect.");
            }

            rep.UpdatePassword(user.Id, passwordHasher.Hash(request.NewPassword), DateTime.UtcNow);
            return StatusCode(HttpStatusCode.NoContent);
        }

    }
}
