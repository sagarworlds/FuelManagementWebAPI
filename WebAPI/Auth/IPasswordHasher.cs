namespace WebAPI.Auth
{
    /// <summary>
    /// Hashes passwords for storage and checks passwords against stored hashes.
    /// </summary>
    public interface IPasswordHasher
    {
        /// <summary>
        /// Hashes a password with a new random salt.
        /// </summary>
        /// <param name="password">The plain-text password.</param>
        /// <returns>The hash to store.</returns>
        string Hash(string password);

        /// <summary>
        /// Checks a password against a stored hash.
        /// </summary>
        /// <param name="password">The password the user entered.</param>
        /// <param name="passwordHash">
        /// The stored hash, or null when there is no such user; the check then takes as long as a real
        /// one, so response times don't reveal which emails are registered.
        /// </param>
        /// <returns>True only when the password matches the hash.</returns>
        bool Verify(string password, string passwordHash);

        /// <summary>
        /// Whether a stored value is a hash produced by <see cref="Hash"/>, as opposed to a legacy plain-text password.
        /// </summary>
        bool IsHashed(string value);
    }
}
