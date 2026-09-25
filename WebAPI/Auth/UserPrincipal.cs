using System;
using System.Globalization;
using System.Security.Claims;
using System.Security.Principal;

namespace WebAPI.Auth
{
    /// <summary>
    /// Creates and reads the principal that represents a signed-in user.
    /// </summary>
    public static class UserPrincipal
    {
        /// <summary>Authentication type recorded on principals created from bearer tokens.</summary>
        public const string AuthenticationType = "Bearer";

        /// <summary>
        /// Creates an authenticated principal for a user.
        /// </summary>
        /// <param name="userId">Id of the signed-in user.</param>
        /// <returns>A principal whose name identifier claim holds <paramref name="userId"/>.</returns>
        public static ClaimsPrincipal Create(int userId)
        {
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString(CultureInfo.InvariantCulture)) };
            return new ClaimsPrincipal(new ClaimsIdentity(claims, AuthenticationType));
        }

        /// <summary>
        /// Reads the signed-in user's id from a principal created by <see cref="Create"/>.
        /// </summary>
        /// <param name="principal">The request's principal.</param>
        /// <returns>The user id.</returns>
        /// <exception cref="InvalidOperationException">
        /// When the principal carries no user id, which means the action is not protected by [Authorize].
        /// </exception>
        public static int GetUserId(this IPrincipal principal)
        {
            var claimsPrincipal = principal as ClaimsPrincipal;
            var claim = claimsPrincipal == null ? null : claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier);
            int userId;
            if (claim == null || !int.TryParse(claim.Value, NumberStyles.None, CultureInfo.InvariantCulture, out userId))
            {
                throw new InvalidOperationException(
                    "The request has no signed-in user. Actions that read the user id must not be marked [AllowAnonymous].");
            }
            return userId;
        }
    }
}
