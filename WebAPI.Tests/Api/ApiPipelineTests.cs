using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
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
        private const string AllowedOrigin = "http://localhost:4200";

        private FakeCustomerRepository repository;
        private JwtTokenService tokenService;
        // Lowest bcrypt cost, to keep the tests fast.
        private readonly BCryptPasswordHasher passwordHasher = new BCryptPasswordHasher(4);
        private HttpServer server;
        private HttpClient client;

        [SetUp]
        public void SetUp()
        {
            repository = new FakeCustomerRepository();
            repository.Users.Add(new User { Id = 1, Email = "one@example.com", Password = passwordHasher.Hash("password-one") });
            repository.Users.Add(new User { Id = 2, Email = "two@example.com", Password = passwordHasher.Hash("password-two") });
            // Unspecified kind, as the old SQLite connection returned dates.
            repository.FuelDetails.Add(new FuelDetail { Id = 10, UserId = 1, MeterReading = 1000, TotalPrice = 500, AddedFuel = 5, CreatedAt = new DateTime(2026, 1, 15, 8, 30, 0, DateTimeKind.Unspecified) });
            repository.FuelDetails.Add(new FuelDetail { Id = 20, UserId = 2, MeterReading = 2000, TotalPrice = 900, AddedFuel = 9 });

            tokenService = new JwtTokenService(Secret, TimeSpan.FromHours(1));
            var config = new HttpConfiguration();
            WebApiConfig.Configure(config, () => repository, tokenService, passwordHasher, new[] { AllowedOrigin });
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
                new FuelDetail { UserId = 2, MeterReading = 1100, TotalPrice = 600, AddedFuel = 6, CreatedAt = DateTime.UtcNow.Date },
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
        public async Task UserList_IsNotAvailable_EvenWhenSignedIn()
        {
            // It returned every user's email and password.
            var response = await client.SendAsync(Request(HttpMethod.Get, "api/User/Get", 1));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task Login_IgnoresTheLetterCaseOfTheEmail()
        {
            var response = await client.SendAsync(LoginRequest("ONE@Example.com", "password-one"));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task Login_WithUnknownEmail_IsUnauthorized()
        {
            var response = await client.SendAsync(LoginRequest("nobody@example.com", "password-one"));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        [Test]
        public async Task Login_DoesNotAcceptAPlainTextStoredPassword()
        {
            repository.Users.Add(new User { Id = 3, Email = "legacy@example.com", Password = "plain-text-password" });

            var response = await client.SendAsync(LoginRequest("legacy@example.com", "plain-text-password"));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        [Test]
        public async Task Register_StoresAHash_AndNeverReturnsThePassword()
        {
            var response = await client.SendAsync(RegisterRequest("new@example.com", "long-enough-password"));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var stored = repository.Users.Last().Password;
            Assert.That(stored, Is.Not.EqualTo("long-enough-password"));
            Assert.That(passwordHasher.Verify("long-enough-password", stored), Is.True);
            var body = JObject.Parse(await response.Content.ReadAsStringAsync());
            Assert.That(body["Password"], Is.Null);
            Assert.That((string)body["Email"], Is.EqualTo("new@example.com"));
        }

        [Test]
        public async Task ChangePassword_ReplacesThePassword()
        {
            var response = await client.SendAsync(ChangePasswordCall("password-one", "new-password-one", 1));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            Assert.That((await client.SendAsync(LoginRequest("one@example.com", "new-password-one"))).StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That((await client.SendAsync(LoginRequest("one@example.com", "password-one"))).StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        [Test]
        public async Task ChangePassword_WithWrongCurrentPassword_IsBadRequest_NotUnauthorized()
        {
            var response = await client.SendAsync(ChangePasswordCall("wrong", "new-password-one", 1));

            // 401 would make the app treat the session as expired and sign the user out.
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(await response.Content.ReadAsStringAsync(), Does.Contain("The current password is incorrect."));
            Assert.That(passwordHasher.Verify("password-one", repository.Users.First().Password), Is.True);
        }

        [Test]
        public async Task ChangePassword_WithShortNewPassword_IsBadRequest()
        {
            var response = await client.SendAsync(ChangePasswordCall("password-one", "short", 1));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(await response.Content.ReadAsStringAsync(), Does.Contain("NewPassword must be 8 to 100 characters."));
        }

        [Test]
        public async Task ChangePassword_WithoutToken_IsUnauthorized()
        {
            var response = await client.SendAsync(ChangePasswordCall("password-one", "new-password-one", null));

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

        [Test]
        public async Task Save_WithNonPositiveValues_IsBadRequest()
        {
            var response = await client.SendAsync(JsonRequest(HttpMethod.Post, "api/FuelDetail/Save",
                "{\"MeterReading\":0,\"TotalPrice\":-1,\"AddedFuel\":0,\"CreatedAt\":\"2026-01-15T00:00:00Z\"}", 1));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            var body = await response.Content.ReadAsStringAsync();
            Assert.That(body, Does.Contain("MeterReading must be a positive whole number."));
            Assert.That(body, Does.Contain("TotalPrice must be greater than 0."));
            Assert.That(body, Does.Contain("AddedFuel must be greater than 0."));
            Assert.That(repository.FuelDetails.Count, Is.EqualTo(2), "Nothing may be saved.");
        }

        [Test]
        public async Task Save_WithTextForANumber_IsBadRequest_InsteadOfStoringZero()
        {
            var response = await client.SendAsync(JsonRequest(HttpMethod.Post, "api/FuelDetail/Save",
                "{\"MeterReading\":\"abc\",\"TotalPrice\":500,\"AddedFuel\":5,\"CreatedAt\":\"2026-01-15T00:00:00Z\"}", 1));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(repository.FuelDetails.Count, Is.EqualTo(2), "Nothing may be saved.");
        }

        [Test]
        public async Task Save_WithoutCreatedAt_IsBadRequest()
        {
            var response = await client.SendAsync(JsonRequest(HttpMethod.Post, "api/FuelDetail/Save",
                "{\"MeterReading\":1100,\"TotalPrice\":500,\"AddedFuel\":5}", 1));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(await response.Content.ReadAsStringAsync(), Does.Contain("CreatedAt is required."));
        }

        [Test]
        public async Task Save_WithFutureCreatedAt_IsBadRequest()
        {
            var future = DateTime.UtcNow.AddDays(3).ToString("o");
            var response = await client.SendAsync(JsonRequest(HttpMethod.Post, "api/FuelDetail/Save",
                "{\"MeterReading\":1100,\"TotalPrice\":500,\"AddedFuel\":5,\"CreatedAt\":\"" + future + "\"}", 1));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(await response.Content.ReadAsStringAsync(), Does.Contain("CreatedAt cannot be in the future."));
        }

        [Test]
        public async Task Save_StoresCreatedAtInUtc_AndSetsModifiedAtOnTheServer()
        {
            var response = await client.SendAsync(JsonRequest(HttpMethod.Post, "api/FuelDetail/Save",
                "{\"MeterReading\":1100,\"TotalPrice\":500,\"AddedFuel\":5,\"CreatedAt\":\"2026-01-15T00:00:00+05:30\",\"ModifiedAt\":\"2000-01-01T00:00:00Z\"}", 1));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var saved = repository.FuelDetails.Last();
            Assert.That(saved.CreatedAt, Is.EqualTo(new DateTime(2026, 1, 14, 18, 30, 0, DateTimeKind.Utc)));
            Assert.That(saved.CreatedAt.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(saved.ModifiedAt, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromMinutes(1)));
        }

        [Test]
        public async Task ResponseDates_AreMarkedAsUtc()
        {
            var response = await client.SendAsync(Request(HttpMethod.Get, "api/FuelDetail/Get", 1));

            Assert.That(await response.Content.ReadAsStringAsync(), Does.Contain("\"CreatedAt\":\"2026-01-15T08:30:00Z\""));
        }

        [TestCase("not-an-email", "long-enough-password", "Email must be a valid email address.")]
        [TestCase("new@example.com", "short", "Password must be 8 to 100 characters.")]
        [TestCase("", "long-enough-password", "Email is required.")]
        public async Task Register_WithInvalidCredentials_IsBadRequest(string email, string password, string message)
        {
            var response = await client.SendAsync(RegisterRequest(email, password));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(await response.Content.ReadAsStringAsync(), Does.Contain(message));
            Assert.That(repository.Users.Count, Is.EqualTo(2), "Nobody may be registered.");
        }

        [Test]
        public async Task Register_WithTakenEmail_IsConflict_IgnoringCase()
        {
            var response = await client.SendAsync(RegisterRequest("ONE@example.com", "another-password"));

            Assert.That(response.StatusCode, Is.EqualTo((HttpStatusCode)409));
            Assert.That(repository.Users.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task Register_SetsTheCreationTimeOnTheServer()
        {
            var response = await client.SendAsync(RegisterRequest("new@example.com", "long-enough-password"));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var user = repository.Users.Last();
            Assert.That(user.CreatedAt, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromMinutes(1)));
            Assert.That(user.ModifiedAt, Is.EqualTo(user.CreatedAt));
        }

        [Test]
        public async Task CorsPreflight_FromAnotherOrigin_IsRefused()
        {
            var request = Request(HttpMethod.Options, "api/FuelDetail/Get");
            request.Headers.Add("Origin", "https://evil.example");
            request.Headers.Add("Access-Control-Request-Method", "GET");
            request.Headers.Add("Access-Control-Request-Headers", "authorization");

            var response = await client.SendAsync(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
            Assert.That(response.Headers.Contains("Access-Control-Allow-Origin"), Is.False);
            Assert.That(response.Headers.Contains("Access-Control-Allow-Headers"), Is.False);
        }

        [Test]
        public async Task ResponseToAnotherOrigin_HasNoAllowOriginHeader()
        {
            var request = Request(HttpMethod.Get, "api/FuelDetail/Get", 1);
            request.Headers.Add("Origin", "https://evil.example");

            var response = await client.SendAsync(request);

            // The browser then hides the response from the calling page.
            Assert.That(response.Headers.Contains("Access-Control-Allow-Origin"), Is.False);
            Assert.That(response.Headers.Vary, Does.Contain("Origin"));
        }

        [Test]
        public async Task CorsPreflight_WithoutRequestedHeaders_Succeeds()
        {
            // This used to throw: the handler read Access-Control-Request-Headers without checking it was sent.
            var request = Request(HttpMethod.Options, "api/FuelDetail/Get");
            request.Headers.Add("Origin", AllowedOrigin);
            request.Headers.Add("Access-Control-Request-Method", "DELETE");

            var response = await client.SendAsync(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Headers.GetValues("Access-Control-Allow-Methods").Single(), Is.EqualTo("DELETE"));
            Assert.That(response.Headers.Contains("Access-Control-Allow-Headers"), Is.False);
        }

        private HttpRequestMessage JsonRequest(HttpMethod method, string path, string json, int? userId = null)
        {
            var request = Request(method, path, userId);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return request;
        }

        private HttpRequestMessage ChangePasswordCall(string currentPassword, string newPassword, int? userId)
        {
            var request = Request(HttpMethod.Post, "api/User/ChangePassword", userId);
            request.Content = new ObjectContent<ChangePasswordRequest>(
                new ChangePasswordRequest { CurrentPassword = currentPassword, NewPassword = newPassword },
                new System.Net.Http.Formatting.JsonMediaTypeFormatter());
            return request;
        }

        private HttpRequestMessage RegisterRequest(string email, string password)
        {
            var request = Request(HttpMethod.Post, "api/User/Save");
            request.Content = new ObjectContent<User>(
                new User { Email = email, Password = password },
                new System.Net.Http.Formatting.JsonMediaTypeFormatter());
            return request;
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
