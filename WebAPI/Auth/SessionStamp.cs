using System;
using System.Security.Cryptography;
using System.Text;

namespace WebAPI.Auth
{
    /// <summary>
    /// Ties tokens to the password they were issued under, so changing the password signs out every
    /// earlier session.
    /// </summary>
    /// <remarks>
    /// A token carries the stamp of the user's password hash at login. Changing the password changes
    /// the hash, and so the stamp, and older tokens no longer match. The stamp is a truncated SHA-256
    /// of the (salted bcrypt) hash, so the readable token payload reveals nothing useful about it.
    /// </remarks>
    public static class SessionStamp
    {
        /// <summary>
        /// Derives the stamp for a stored password hash: the first 128 bits of its SHA-256, base64url-encoded.
        /// </summary>
        /// <exception cref="ArgumentNullException">When <paramref name="passwordHash"/> is null.</exception>
        public static string For(string passwordHash)
        {
            if (passwordHash == null)
            {
                throw new ArgumentNullException("passwordHash");
            }
            using (var sha256 = SHA256.Create())
            {
                var digest = sha256.ComputeHash(Encoding.UTF8.GetBytes(passwordHash));
                return Convert.ToBase64String(digest, 0, 16).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            }
        }

        /// <summary>
        /// Whether a token's stamp still matches the user's current password hash.
        /// </summary>
        /// <returns>False when either value is missing, e.g. a token issued before stamps existed.</returns>
        public static bool Matches(string tokenStamp, string currentPasswordHash)
        {
            return tokenStamp != null && currentPasswordHash != null
                && string.Equals(tokenStamp, For(currentPasswordHash), StringComparison.Ordinal);
        }
    }
}
