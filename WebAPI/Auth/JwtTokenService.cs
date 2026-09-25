using System;
using System.Diagnostics;
using System.Globalization;
using System.IdentityModel;
using System.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace WebAPI.Auth
{
    /// <summary>
    /// Issues and validates JSON Web Tokens signed with HMAC-SHA256.
    /// </summary>
    public class JwtTokenService : ITokenService
    {
        /// <summary>Issuer and audience written into, and required on, every token.</summary>
        public const string Issuer = "FuelManagementWebAPI";

        /// <summary>Claim that carries the <see cref="SessionStamp"/>.</summary>
        public const string SessionStampClaim = "stamp";

        // HMAC-SHA256 needs a key at least as long as its 256-bit output to be full strength.
        private const int MinimumSecretBytes = 32;

        private readonly byte[] key;
        private readonly TimeSpan lifetime;
        private readonly Func<DateTime> utcNow;

        /// <summary>
        /// Creates a service that stamps tokens with the current UTC time.
        /// </summary>
        /// <param name="secret">Signing secret; at least 32 bytes when UTF-8 encoded.</param>
        /// <param name="lifetime">How long an issued token stays valid.</param>
        /// <exception cref="ArgumentNullException">When <paramref name="secret"/> is null.</exception>
        /// <exception cref="ArgumentException">When <paramref name="secret"/> is too short.</exception>
        /// <exception cref="ArgumentOutOfRangeException">When <paramref name="lifetime"/> is not positive.</exception>
        public JwtTokenService(string secret, TimeSpan lifetime)
            : this(secret, lifetime, () => DateTime.UtcNow)
        {
        }

        /// <summary>
        /// Creates a service with an explicit clock, so tests can issue already-expired tokens.
        /// </summary>
        /// <param name="secret">Signing secret; at least 32 bytes when UTF-8 encoded.</param>
        /// <param name="lifetime">How long an issued token stays valid.</param>
        /// <param name="utcNow">Returns the current UTC time.</param>
        /// <exception cref="ArgumentNullException">When <paramref name="secret"/> or <paramref name="utcNow"/> is null.</exception>
        /// <exception cref="ArgumentException">When <paramref name="secret"/> is too short.</exception>
        /// <exception cref="ArgumentOutOfRangeException">When <paramref name="lifetime"/> is not positive.</exception>
        public JwtTokenService(string secret, TimeSpan lifetime, Func<DateTime> utcNow)
        {
            if (secret == null)
            {
                throw new ArgumentNullException("secret");
            }
            if (utcNow == null)
            {
                throw new ArgumentNullException("utcNow");
            }

            key = Encoding.UTF8.GetBytes(secret);
            if (key.Length < MinimumSecretBytes)
            {
                throw new ArgumentException(
                    string.Format(CultureInfo.InvariantCulture, "The JWT secret must be at least {0} bytes long.", MinimumSecretBytes),
                    "secret");
            }
            if (lifetime <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException("lifetime", "The token lifetime must be positive.");
            }

            this.lifetime = lifetime;
            this.utcNow = utcNow;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentException">When <paramref name="sessionStamp"/> is null or empty.</exception>
        public IssuedToken Issue(int userId, string sessionStamp)
        {
            if (string.IsNullOrEmpty(sessionStamp))
            {
                throw new ArgumentException("A session stamp is required.", "sessionStamp");
            }

            var issuedAt = utcNow();
            var expiresAt = issuedAt.Add(lifetime);
            var credentials = new SigningCredentials(
                new InMemorySymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature,
                SecurityAlgorithms.Sha256Digest);
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString(CultureInfo.InvariantCulture)),
                new Claim(SessionStampClaim, sessionStamp)
            };

            var token = new JwtSecurityToken(Issuer, Issuer, claims, issuedAt, expiresAt, credentials);
            return new IssuedToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
        }

        /// <inheritdoc />
        public TokenIdentity Validate(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            var parameters = new TokenValidationParameters
            {
                ValidIssuer = Issuer,
                ValidAudience = Issuer,
                IssuerSigningKey = new InMemorySymmetricSecurityKey(key),
                // The same server issues and validates tokens, so there is no clock drift to allow for.
                ClockSkew = TimeSpan.Zero
            };

            ClaimsPrincipal principal;
            try
            {
                SecurityToken validatedToken;
                principal = new JwtSecurityTokenHandler().ValidateToken(token, parameters, out validatedToken);
            }
            catch (SignatureVerificationFailedException ex)
            {
                // Signed with another key or altered. Only the type is logged: the messages echo the token.
                Trace.TraceWarning("Rejected bearer token: {0}", ex.GetType().Name);
                return null;
            }
            catch (SecurityTokenException ex)
            {
                // Expired, not yet valid, unsigned, or wrong issuer/audience.
                Trace.TraceWarning("Rejected bearer token: {0}", ex.GetType().Name);
                return null;
            }
            catch (ArgumentException ex)
            {
                // Not a well-formed JWT.
                Trace.TraceWarning("Rejected bearer token: {0}", ex.GetType().Name);
                return null;
            }

            // The handler maps the "sub" claim to ClaimTypes.NameIdentifier on the way in.
            var subject = principal.FindFirst(ClaimTypes.NameIdentifier);
            int userId;
            if (subject == null || !int.TryParse(subject.Value, NumberStyles.None, CultureInfo.InvariantCulture, out userId))
            {
                Trace.TraceWarning("Rejected bearer token: missing or non-numeric subject.");
                return null;
            }
            var stamp = principal.FindFirst(SessionStampClaim);
            return new TokenIdentity(userId, stamp == null ? null : stamp.Value);
        }
    }
}
