using System.Net;
using System.Net.Http.Json;
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

    [Fact]
    public async Task Register_ThenLogin_ReturnsValidToken()
    {
        var email = $"{Guid.NewGuid():N}@flowbox.test";
        var registerReq = new RegisterEndpoint.RegisterRequest(email, "SecurePass123!", "Customer");

        var registerRes = await _client.PostAsJsonAsync("/api/auth/register", registerReq);
        registerRes.StatusCode.Should().Be(HttpStatusCode.Created);

        var loginReq = new LoginEndpoint.LoginRequest(email, "SecurePass123!");
        var loginRes = await _client.PostAsJsonAsync("/api/auth/login", loginReq);
        loginRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginBody = await loginRes.Content.ReadFromJsonAsync<LoginEndpoint.LoginResponse>();
        loginBody.Should().NotBeNull();
        loginBody!.Token.Should().NotBeNullOrWhiteSpace();
        loginBody.Role.Should().Be("Customer");
    }

    [Fact]
    public async Task Register_WithAlreadyUsedEmail_ReturnsValidationProblem()
    {
        var email = $"{Guid.NewGuid():N}@flowbox.test";
        var request = new RegisterEndpoint.RegisterRequest(email, "SecurePass123!", "Customer");

        var first = await _client.PostAsJsonAsync("/api/auth/register", request);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await _client.PostAsJsonAsync("/api/auth/register", request);
        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var email = $"{Guid.NewGuid():N}@flowbox.test";
        await _client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterEndpoint.RegisterRequest(email, "SecurePass123!", "Customer"));

        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginEndpoint.LoginRequest(email, "WrongPassword!"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateCourier_WithoutToken_ReturnsUnauthorized()
    {
        var request = new CreateCourierEndpoint.CreateCourierRequest("Test Kurye", "+905551234567");

        var response = await _client.PostAsJsonAsync("/api/couriers", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateCourier_AsCustomer_ReturnsForbidden()
    {
        // Kurye oluşturma sadece Admin'e açık — Customer rolü yetkili olmamalı.
        await _client.AuthenticateAsAsync(UserRole.Customer);

        var request = new CreateCourierEndpoint.CreateCourierRequest("Test Kurye", "+905551234567");
        var response = await _client.PostAsJsonAsync("/api/couriers", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
