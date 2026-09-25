using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using WebAPI.Auth;
using WebAPI.Model;
using WebAPI.Tests.Fakes;

namespace WebAPI.Tests.Api
{
    /// <summary>
    /// Runs requests through the real Web API configuration (routes, CORS handler, authentication
    /// and authorization filters, controllers) in memory, with a fake repository.
    /// </summary>
    [TestFixture]
    public class ApiPipelineTests
    {
        private const string Secret = "test-secret-that-is-at-least-32-bytes-long";

        private FakeCustomerRepository repository;
        private JwtTokenService tokenService;
        private HttpServer server;
        private HttpClient client;

        [SetUp]
        public void SetUp()
        {
            repository = new FakeCustomerRepository();
            repository.Users.Add(new User { Id = 1, Email = "one@example.com", Password = "password-one" });
            repository.Users.Add(new User { Id = 2, Email = "two@example.com", Password = "password-two" });
            repository.FuelDetails.Add(new FuelDetail { Id = 10, UserId = 1, MeterReading = 1000, TotalPrice = 500, AddedFuel = 5 });
            repository.FuelDetails.Add(new FuelDetail { Id = 20, UserId = 2, MeterReading = 2000, TotalPrice = 900, AddedFuel = 9 });

            tokenService = new JwtTokenService(Secret, TimeSpan.FromHours(1));
            var config = new HttpConfiguration();
            WebApiConfig.Configure(config, () => repository, tokenService);
            config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;
            server = new HttpServer(config);
            client = new HttpClient(server);
        }

        [TearDown]
        public void TearDown()
        {
            client.Dispose();
            server.Dispose();
        }

        [Test]
        public async Task FuelList_WithoutToken_IsUnauthorized()
        {
            var response = await client.SendAsync(Request(HttpMethod.Get, "api/FuelDetail/Get"));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        [Test]
        public async Task FuelList_WithInvalidToken_IsUnauthorized()
        {
            var request = Request(HttpMethod.Get, "api/FuelDetail/Get");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-token");

            var response = await client.SendAsync(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        [Test]
        public async Task FuelList_ReturnsOnlyTheCallersEntries()
        {
            var response = await client.SendAsync(Request(HttpMethod.Get, "api/FuelDetail/Get", 1));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var entries = await response.Content.ReadAsAsync<FuelDetail[]>();
            Assert.That(entries.Select(e => e.Id), Is.EqualTo(new[] { 10 }));
        }

        [Test]
        public async Task EntriesOfAnotherUserId_AreForbidden()
        {
            var response = await client.SendAsync(Request(HttpMethod.Get, "api/FuelDetail/GetByUserId?UserId=2", 1));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        }

        [Test]
        public async Task EntriesOfOwnUserId_AreReturned()
        {
            var response = await client.SendAsync(Request(HttpMethod.Get, "api/FuelDetail/GetByUserId?UserId=1", 1));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var entries = await response.Content.ReadAsAsync<FuelDetail[]>();
            Assert.That(entries.Select(e => e.Id), Is.EqualTo(new[] { 10 }));
        }

        [Test]
        public async Task AnotherUsersEntryById_IsNotFound()
        {
            var response = await client.SendAsync(Request(HttpMethod.Get, "api/FuelDetail/GetFuelDetailById?Id=20", 1));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task SavedEntry_BelongsToTheTokenUser_NotTheBodyUserId()
        {
            var request = Request(HttpMethod.Post, "api/FuelDetail/Save", 1);
            request.Content = new ObjectContent<FuelDetail>(
                new FuelDetail { UserId = 2, MeterReading = 1100, TotalPrice = 600, AddedFuel = 6 },
                new System.Net.Http.Formatting.JsonMediaTypeFormatter());

            var response = await client.SendAsync(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(repository.FuelDetails.Last().UserId, Is.EqualTo(1));
        }

        [Test]
        public async Task SaveWithoutBody_IsBadRequest()
        {
            var response = await client.SendAsync(Request(HttpMethod.Post, "api/FuelDetail/Save", 1));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task Login_WithValidCredentials_ReturnsATokenForThatUser()
        {
            var response = await client.SendAsync(LoginRequest("two@example.com", "password-two"));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var body = JObject.Parse(await response.Content.ReadAsStringAsync());
            Assert.That(tokenService.ValidateAndGetUserId((string)body["Token"]), Is.EqualTo(2));
            Assert.That((int)body["UserId"], Is.EqualTo(2));
            Assert.That(body["Password"], Is.Null, "The login response must not echo the password.");
        }

        [Test]
        public async Task Login_WithWrongPassword_IsUnauthorized()
        {
            var response = await client.SendAsync(LoginRequest("two@example.com", "wrong"));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        [Test]
        public async Task Login_StillWorks_WhenAStaleTokenIsSent()
        {
            var request = LoginRequest("one@example.com", "password-one");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "expired-or-garbage");

            var response = await client.SendAsync(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task UserList_WithoutToken_IsUnauthorized()
        {
            var response = await client.SendAsync(Request(HttpMethod.Get, "api/User/Get"));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        [Test]
        public async Task CorsPreflight_AllowsTheAuthorizationHeader()
        {
            var request = Request(HttpMethod.Options, "api/FuelDetail/Get");
            request.Headers.Add("Origin", "http://localhost:4200");
            request.Headers.Add("Access-Control-Request-Method", "GET");
            request.Headers.Add("Access-Control-Request-Headers", "authorization");

            var response = await client.SendAsync(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Headers.GetValues("Access-Control-Allow-Headers").Single(), Does.Contain("authorization"));
        }

        [Test]
        public async Task UnauthorizedCorsResponse_StillCarriesTheAllowOriginHeader()
        {
            var request = Request(HttpMethod.Get, "api/FuelDetail/Get");
            request.Headers.Add("Origin", "http://localhost:4200");

            var response = await client.SendAsync(request);

            // Without this header the browser hides the 401 from the app, which then can't redirect to login.
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(response.Headers.GetValues("Access-Control-Allow-Origin").Single(), Is.EqualTo("http://localhost:4200"));
        }

        private HttpRequestMessage Request(HttpMethod method, string path, int? userId = null)
        {
            var request = new HttpRequestMessage(method, "http://localhost/" + path);
            if (userId.HasValue)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenService.Issue(userId.Value).Value);
            }
            return request;
        }

        private HttpRequestMessage LoginRequest(string email, string password)
        {
            var request = Request(HttpMethod.Post, "api/User/Login");
            request.Content = new ObjectContent<User>(
                new User { Email = email, Password = password },
                new System.Net.Http.Formatting.JsonMediaTypeFormatter());
            return request;
        }
    }
}
