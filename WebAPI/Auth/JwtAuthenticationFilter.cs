using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Filters;
using WebAPI.Data;

namespace WebAPI.Auth
{
    /// <summary>
    /// Sets the request's principal from an <c>Authorization: Bearer {token}</c> header.
    /// </summary>
    /// <remarks>
    /// A token only counts while its user exists and still has the password it was issued under
    /// (see <see cref="SessionStamp"/>), so changing the password signs out earlier sessions.
    /// A missing or invalid token leaves the request anonymous instead of failing it here, so
    /// [AllowAnonymous] actions such as login keep working even when a client sends a stale
    /// token. The global <see cref="System.Web.Http.AuthorizeAttribute"/> rejects anonymous
    /// requests everywhere else with 401.
    /// </remarks>
    public class JwtAuthenticationFilter : IAuthenticationFilter
    {
        private const string BearerScheme = "Bearer";
        private static readonly Task Completed = Task.FromResult(0);

        private readonly ITokenService tokenService;
        private readonly Func<ICustomerRepository> repositoryFactory;

        /// <param name="tokenService">Validates the bearer tokens.</param>
        /// <param name="repositoryFactory">Looks up the token's user to check the session is still current.</param>
        /// <exception cref="ArgumentNullException">When either argument is null.</exception>
        public JwtAuthenticationFilter(ITokenService tokenService, Func<ICustomerRepository> repositoryFactory)
        {
            if (tokenService == null)
            {
                throw new ArgumentNullException("tokenService");
            }
            if (repositoryFactory == null)
            {
                throw new ArgumentNullException("repositoryFactory");
            }
            this.tokenService = tokenService;
            this.repositoryFactory = repositoryFactory;
        }

        /// <inheritdoc />
        public bool AllowMultiple
        {
            get { return false; }
        }

        /// <inheritdoc />
        public Task AuthenticateAsync(HttpAuthenticationContext context, CancellationToken cancellationToken)
        {
            var authorization = context.Request.Headers.Authorization;
            if (authorization != null && string.Equals(authorization.Scheme, BearerScheme, StringComparison.OrdinalIgnoreCase))
            {
                var identity = tokenService.Validate(authorization.Parameter);
                if (identity != null && IsCurrent(identity))
                {
                    context.Principal = UserPrincipal.Create(identity.UserId);
                }
            }
            return Completed;
        }

        /// <summary>
        /// Whether the token's user still exists and hasn't changed password since the token was issued.
        /// </summary>
        private bool IsCurrent(TokenIdentity identity)
        {
            var user = repositoryFactory().GetUserById(identity.UserId);
            return user != null && SessionStamp.Matches(identity.SessionStamp, user.Password);
        }

        /// <inheritdoc />
        public Task ChallengeAsync(HttpAuthenticationChallengeContext context, CancellationToken cancellationToken)
        {
            return Completed;
        }
    }
}
