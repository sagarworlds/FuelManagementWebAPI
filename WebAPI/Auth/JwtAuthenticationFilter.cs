using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Filters;

namespace WebAPI.Auth
{
    /// <summary>
    /// Sets the request's principal from an <c>Authorization: Bearer {token}</c> header.
    /// </summary>
    /// <remarks>
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

        /// <param name="tokenService">Validates the bearer tokens.</param>
        /// <exception cref="ArgumentNullException">When <paramref name="tokenService"/> is null.</exception>
        public JwtAuthenticationFilter(ITokenService tokenService)
        {
            if (tokenService == null)
            {
                throw new ArgumentNullException("tokenService");
            }
            this.tokenService = tokenService;
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
                var userId = tokenService.ValidateAndGetUserId(authorization.Parameter);
                if (userId.HasValue)
                {
                    context.Principal = UserPrincipal.Create(userId.Value);
                }
            }
            return Completed;
        }

        /// <inheritdoc />
        public Task ChallengeAsync(HttpAuthenticationChallengeContext context, CancellationToken cancellationToken)
        {
            return Completed;
        }
    }
}
