using System;
using System.Text.RegularExpressions;

namespace WebAPI.Auth
{
    /// <summary>
    /// Hashes passwords with bcrypt.
    /// </summary>
    public class BCryptPasswordHasher : IPasswordHasher
    {
        /// <summary>
        /// Default cost: 2^12 rounds, roughly a quarter of a second per hash. Slow enough to make guessing
        /// stolen hashes expensive, fast enough for an interactive login.
        /// </summary>
        public const int DefaultWorkFactor = 12;

        // A bcrypt hash: version ($2a$ etc.), two-digit cost, then 53 characters of salt and hash.
        private static readonly Regex HashFormat = new Regex(@"^\$2[abxy]\$\d{2}\$[./A-Za-z0-9]{53}$", RegexOptions.CultureInvariant);

        private readonly int workFactor;
        private readonly Lazy<string> unknownUserHash;

        /// <summary>Creates a hasher with <see cref="DefaultWorkFactor"/>.</summary>
        public BCryptPasswordHasher()
            : this(DefaultWorkFactor)
        {
        }

        /// <param name="workFactor">bcrypt cost (log2 of the rounds), 4 to 31; tests use a low value for speed.</param>
        /// <exception cref="ArgumentOutOfRangeException">When <paramref name="workFactor"/> is outside 4 to 31.</exception>
        public BCryptPasswordHasher(int workFactor)
        {
            if (workFactor < 4 || workFactor > 31)
            {
                throw new ArgumentOutOfRangeException("workFactor", "The bcrypt work factor must be between 4 and 31.");
            }
            this.workFactor = workFactor;
            unknownUserHash = new Lazy<string>(() => Hash(Guid.NewGuid().ToString()));
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">When <paramref name="password"/> is null.</exception>
        public string Hash(string password)
        {
            if (password == null)
            {
                throw new ArgumentNullException("password");
            }
            // Enhanced mode pre-hashes with SHA-384, so characters beyond bcrypt's 72-byte limit still count.
            return BCrypt.Net.BCrypt.EnhancedHashPassword(password, workFactor);
        }

        /// <inheritdoc />
        public bool Verify(string password, string passwordHash)
        {
            if (password == null)
            {
                return false;
            }
            if (passwordHash == null)
            {
                BCrypt.Net.BCrypt.EnhancedVerify(password, unknownUserHash.Value);
                return false;
            }
            return IsHashed(passwordHash) && BCrypt.Net.BCrypt.EnhancedVerify(password, passwordHash);
        }

        /// <inheritdoc />
        public bool IsHashed(string value)
        {
            return value != null && HashFormat.IsMatch(value);
        }
    }
}
