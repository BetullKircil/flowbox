using System.Net;
using System.Net.Http.Json;
using System.Text;
using FlowBox.Api.Endpoints.Auth;
using FlowBox.Api.Endpoints.Courier;
using FlowBox.Api.Enums;
using FluentAssertions;
using Xunit;

namespace FlowBox.IntegrationTests.AuthTest;

public class AuthEndpointTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointTests(IntegrationTestWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static string NewEmail() => $"{Guid.NewGuid():N}@flowbox.test";

    [Fact]
    public async Task Register_ThenLogin_ReturnsValidToken()
    {
        var email = NewEmail();

        var registerRes = await _client.PostAsJsonAsync(
            "/api/auth/register", new RegisterEndpoint.RegisterRequest(email, "SecurePass123!"));
        registerRes.StatusCode.Should().Be(HttpStatusCode.Created);

        var loginRes = await _client.PostAsJsonAsync(
            "/api/auth/login", new LoginEndpoint.LoginRequest(email, "SecurePass123!"));
        loginRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginBody = await loginRes.Content.ReadFromJsonAsync<LoginEndpoint.LoginResponse>();
        loginBody.Should().NotBeNull();
        loginBody!.Token.Should().NotBeNullOrWhiteSpace();
        loginBody.Role.Should().Be("Customer");
    }

    [Fact]
    public async Task Register_WithRoleAdminInBody_StillCreatesCustomer()
    {
        var email = NewEmail();
        var json = $$"""{"email":"{{email}}","password":"SecurePass123!","role":"Admin"}""";

        var registerRes = await _client.PostAsync(
            "/api/auth/register", new StringContent(json, Encoding.UTF8, "application/json"));
        registerRes.StatusCode.Should().Be(HttpStatusCode.Created);

        var registered = await registerRes.Content.ReadFromJsonAsync<RegisterEndpoint.RegisterResponse>();
        registered!.Role.Should().Be("Customer");

        var loginRes = await _client.PostAsJsonAsync(
            "/api/auth/login", new LoginEndpoint.LoginRequest(email, "SecurePass123!"));
        var loginBody = await loginRes.Content.ReadFromJsonAsync<LoginEndpoint.LoginResponse>();
        loginBody!.Role.Should().Be("Customer");

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginBody.Token);
        var courierRes = await _client.PostAsJsonAsync(
            "/api/couriers", new CreateCourierEndpoint.CreateCourierRequest("Test Kurye", "+905551234567"));
        courierRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Register_WithAlreadyUsedEmail_ReturnsValidationProblem()
    {
        var request = new RegisterEndpoint.RegisterRequest(NewEmail(), "SecurePass123!");

        var first = await _client.PostAsJsonAsync("/api/auth/register", request);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await _client.PostAsJsonAsync("/api/auth/register", request);
        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var email = NewEmail();
        await _client.PostAsJsonAsync(
            "/api/auth/register", new RegisterEndpoint.RegisterRequest(email, "SecurePass123!"));

        var response = await _client.PostAsJsonAsync(
            "/api/auth/login", new LoginEndpoint.LoginRequest(email, "WrongPassword!"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SeedAdmin_CanLogin_WithAdminRole()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginEndpoint.LoginRequest(TestAuthHelper.SeedAdminEmail, TestAuthHelper.SeedAdminPassword));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LoginEndpoint.LoginResponse>();
        body!.Role.Should().Be("Admin");
    }

    [Fact]
    public async Task CreateUser_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/admin/users", new CreateUserEndpoint.CreateUserRequest(NewEmail(), "SecurePass123!", "Courier"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateUser_AsCustomer_ReturnsForbidden()
    {
        await _client.AuthenticateAsAsync(UserRole.Customer);

        var response = await _client.PostAsJsonAsync(
            "/api/admin/users", new CreateUserEndpoint.CreateUserRequest(NewEmail(), "SecurePass123!", "Admin"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateUser_AsAdmin_CreatesCourierWhoCanLogin()
    {
        var courierEmail = NewEmail();
        await _client.LoginAsync(TestAuthHelper.SeedAdminEmail, TestAuthHelper.SeedAdminPassword);

        var createRes = await _client.PostAsJsonAsync(
            "/api/admin/users", new CreateUserEndpoint.CreateUserRequest(courierEmail, "SecurePass123!", "Courier"));
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);

        _client.DefaultRequestHeaders.Authorization = null;
        var loginRes = await _client.PostAsJsonAsync(
            "/api/auth/login", new LoginEndpoint.LoginRequest(courierEmail, "SecurePass123!"));
        loginRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await loginRes.Content.ReadFromJsonAsync<LoginEndpoint.LoginResponse>();
        body!.Role.Should().Be("Courier");
    }

    [Fact]
    public async Task CreateUser_AsAdmin_WithInvalidRole_ReturnsValidationProblem()
    {
        await _client.LoginAsync(TestAuthHelper.SeedAdminEmail, TestAuthHelper.SeedAdminPassword);

        var response = await _client.PostAsJsonAsync(
            "/api/admin/users", new CreateUserEndpoint.CreateUserRequest(NewEmail(), "SecurePass123!", "SuperUser"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCourier_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/couriers", new CreateCourierEndpoint.CreateCourierRequest("Test Kurye", "+905551234567"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateCourier_AsCustomer_ReturnsForbidden()
    {
        await _client.AuthenticateAsAsync(UserRole.Customer);

        var response = await _client.PostAsJsonAsync(
            "/api/couriers", new CreateCourierEndpoint.CreateCourierRequest("Test Kurye", "+905551234567"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
