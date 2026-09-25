using System;

namespace WebAPI.Auth
{
    /// <summary>
    /// A newly issued bearer token.
    /// </summary>
    public sealed class IssuedToken
    {
        /// <param name="value">The encoded token.</param>
        /// <param name="expiresAtUtc">UTC time after which the token is rejected.</param>
        /// <exception cref="ArgumentException">When <paramref name="value"/> is null or empty.</exception>
        public IssuedToken(string value, DateTime expiresAtUtc)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException("A token value is required.", "value");
            }

            Value = value;
            ExpiresAtUtc = expiresAtUtc;
        }

        /// <summary>The encoded token, sent by clients as <c>Authorization: Bearer {Value}</c>.</summary>
        public string Value { get; private set; }

        /// <summary>UTC time after which the token is rejected.</summary>
        public DateTime ExpiresAtUtc { get; private set; }
    }
}
