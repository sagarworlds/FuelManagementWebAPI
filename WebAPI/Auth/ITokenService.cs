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
        /// <returns>The token and when it expires.</returns>
        IssuedToken Issue(int userId);

        /// <summary>
        /// Validates a token's signature, issuer, audience and lifetime.
        /// </summary>
        /// <param name="token">The raw token, without the "Bearer " prefix.</param>
        /// <returns>The id of the user the token was issued to, or null when the token is not valid.</returns>
        int? ValidateAndGetUserId(string token);
    }
}
