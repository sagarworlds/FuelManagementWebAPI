namespace WebAPI.Auth
{
    /// <summary>
    /// Issues and validates the bearer tokens that identify signed-in users.
    /// </summary>
    public interface ITokenService
    {
        /// <summary>
        /// Creates a signed token for a user.
        /// </summary>
        /// <param name="userId">Id of the user the token identifies.</param>
        /// <param name="sessionStamp">The user's current <see cref="SessionStamp"/>, checked on every request.</param>
        /// <returns>The token and when it expires.</returns>
        IssuedToken Issue(int userId, string sessionStamp);

        /// <summary>
        /// Validates a token's signature, issuer, audience and lifetime.
        /// </summary>
        /// <param name="token">The raw token, without the "Bearer " prefix.</param>
        /// <returns>The user and session stamp the token was issued for, or null when the token is not valid.</returns>
        TokenIdentity Validate(string token);
    }
}
