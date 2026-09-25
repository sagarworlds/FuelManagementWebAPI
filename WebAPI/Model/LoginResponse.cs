using System;

namespace WebAPI.Model
{
    /// <summary>
    /// Result of a successful login.
    /// </summary>
    public class LoginResponse
    {
        /// <summary>Bearer token to send as <c>Authorization: Bearer {Token}</c>.</summary>
        public string Token { get; set; }

        /// <summary>UTC time after which the token is rejected and the user must log in again.</summary>
        public DateTime ExpiresAt { get; set; }

        public int UserId { get; set; }

        public string Email { get; set; }
    }
}
