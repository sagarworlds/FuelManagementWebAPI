using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace WebAPI.Tests.Api
{
    [TestFixture]
    public class CrossDomainHandlerTests
    {
        [TestCase("http://localhost:4200")]
        [TestCase("HTTP://LOCALHOST:4200")]
        public void ListedOrigin_IsAllowed_IgnoringCase(string origin)
        {
            Assert.That(AllowOriginFor(new[] { "http://localhost:4200" }, origin), Is.EqualTo(origin));
        }

        [Test]
        public void ConfiguredOriginWithTrailingSlash_StillMatches()
        {
            Assert.That(AllowOriginFor(new[] { "https://fuel.example.com/" }, "https://fuel.example.com"), Is.EqualTo("https://fuel.example.com"));
        }

        [TestCase("http://localhost:4201")]
        [TestCase("https://localhost:4200")]
        [TestCase("http://localhost:4200.evil.example")]
        public void UnlistedOrigin_IsNotAllowed(string origin)
        {
            Assert.That(AllowOriginFor(new[] { "http://localhost:4200" }, origin), Is.Null);
        }

        [Test]
        public void Wildcard_AllowsAnyOrigin()
        {
            Assert.That(AllowOriginFor(new[] { "*" }, "https://anything.example"), Is.EqualTo("https://anything.example"));
        }

        [Test]
        public void RequestWithoutOrigin_IsPassedThroughUntouched()
        {
            var response = Send(new[] { "*" }, new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/x"));

            Assert.That(response.Headers.Contains("Access-Control-Allow-Origin"), Is.False);
            Assert.That(response.Headers.Vary, Is.Empty);
        }

        [Test]
        public void ParseOrigins_SplitsAndTrims()
        {
            Assert.That(CrossDomainHandler.ParseOrigins(" http://a.example , https://b.example,, "),
                Is.EqualTo(new[] { "http://a.example", "https://b.example" }));
            Assert.That(CrossDomainHandler.ParseOrigins(null), Is.Empty);
        }

        private static string AllowOriginFor(string[] allowedOrigins, string origin)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/x");
            request.Headers.Add("Origin", origin);
            var response = Send(allowedOrigins, request);
            return response.Headers.Contains("Access-Control-Allow-Origin")
                ? response.Headers.GetValues("Access-Control-Allow-Origin").Single()
                : null;
        }

        private static HttpResponseMessage Send(string[] allowedOrigins, HttpRequestMessage request)
        {
            var handler = new CrossDomainHandler(allowedOrigins) { InnerHandler = new OkHandler() };
            return new HttpMessageInvoker(handler).SendAsync(request, CancellationToken.None).Result;
        }

        private class OkHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }
        }
    }
}
