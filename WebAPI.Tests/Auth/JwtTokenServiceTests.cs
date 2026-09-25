using System;
using System.Text;
using NUnit.Framework;
using WebAPI.Auth;

namespace WebAPI.Tests.Auth
{
    [TestFixture]
    public class JwtTokenServiceTests
    {
        private const string Secret = "test-secret-that-is-at-least-32-bytes-long";
        private static readonly TimeSpan OneHour = TimeSpan.FromHours(1);

        [Test]
        public void IssuedToken_ValidatesToTheSameUser()
        {
            var service = new JwtTokenService(Secret, OneHour);

            var token = service.Issue(42);

            Assert.That(service.ValidateAndGetUserId(token.Value), Is.EqualTo(42));
        }

        [Test]
        public void IssuedToken_ExpiresAfterTheLifetime()
        {
            var now = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);
            var service = new JwtTokenService(Secret, OneHour, () => now);

            var token = service.Issue(42);

            Assert.That(token.ExpiresAtUtc, Is.EqualTo(now.Add(OneHour)));
        }

        [Test]
        public void ExpiredToken_IsRejected()
        {
            var twoHoursAgo = DateTime.UtcNow.AddHours(-2);
            var token = new JwtTokenService(Secret, OneHour, () => twoHoursAgo).Issue(42);

            Assert.That(new JwtTokenService(Secret, OneHour).ValidateAndGetUserId(token.Value), Is.Null);
        }

        [Test]
        public void TokenSignedWithAnotherSecret_IsRejected()
        {
            var token = new JwtTokenService("another-secret-that-is-at-least-32-bytes", OneHour).Issue(42);

            Assert.That(new JwtTokenService(Secret, OneHour).ValidateAndGetUserId(token.Value), Is.Null);
        }

        [Test]
        public void TokenWithAlteredUser_IsRejected()
        {
            var service = new JwtTokenService(Secret, OneHour);
            var parts = service.Issue(1).Value.Split('.');
            var payload = Encoding.UTF8.GetString(Base64UrlDecode(parts[1])).Replace("\"sub\":\"1\"", "\"sub\":\"2\"");
            var forged = parts[0] + "." + Base64UrlEncode(Encoding.UTF8.GetBytes(payload)) + "." + parts[2];

            Assert.That(service.ValidateAndGetUserId(forged), Is.Null);
        }

        [Test]
        public void UnsignedToken_IsRejected()
        {
            var service = new JwtTokenService(Secret, OneHour);
            var payload = service.Issue(42).Value.Split('.')[1];
            var header = Base64UrlEncode(Encoding.UTF8.GetBytes("{\"alg\":\"none\",\"typ\":\"JWT\"}"));

            Assert.That(service.ValidateAndGetUserId(header + "." + payload + "."), Is.Null);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("not-a-token")]
        [TestCase("a.b.c")]
        public void MalformedToken_IsRejected(string token)
        {
            Assert.That(new JwtTokenService(Secret, OneHour).ValidateAndGetUserId(token), Is.Null);
        }

        [Test]
        public void ShortSecret_IsRefused()
        {
            Assert.Throws<ArgumentException>(() => new JwtTokenService("too-short", OneHour));
        }

        [Test]
        public void NonPositiveLifetime_IsRefused()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new JwtTokenService(Secret, TimeSpan.Zero));
        }

        private static string Base64UrlEncode(byte[] bytes)
        {
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static byte[] Base64UrlDecode(string text)
        {
            var base64 = text.Replace('-', '+').Replace('_', '/');
            return Convert.FromBase64String(base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '='));
        }
    }
}
