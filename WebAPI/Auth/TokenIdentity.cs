namespace WebAPI.Auth
{
    /// <summary>
    /// What a valid token says about its holder.
    /// </summary>
    public sealed class TokenIdentity
    {
        /// <param name="userId">The user the token was issued to.</param>
        /// <param name="sessionStamp">The <see cref="SessionStamp"/> at issue time, or null for tokens without one.</param>
        public TokenIdentity(int userId, string sessionStamp)
        {
            UserId = userId;
            SessionStamp = sessionStamp;
        }

        public int UserId { get; private set; }

        public string SessionStamp { get; private set; }
    }
}
