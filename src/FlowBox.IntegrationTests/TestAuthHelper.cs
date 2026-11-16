using System.Net.Http.Headers;
using System.Net.Http.Json;
using FlowBox.Api.Endpoints.Auth;
using FlowBox.Api.Endpoints.Shipment;
using FlowBox.Api.Enums;

namespace FlowBox.IntegrationTests;

public record TestCourier(Guid CourierId, string Email, string Password);

public static class TestAuthHelper
{
    public const string SeedAdminEmail = "seed-admin@flowbox.test";
    public const string SeedAdminPassword = "SeedAdminPass123!";

    private const string TestPassword = "TestPassword123!";

    public static async Task AuthenticateAsAsync(this HttpClient client, UserRole role)
    {
        if (role == UserRole.Courier)
        {
            var courier = await client.CreateCourierAsync();
            await client.LoginAsync(courier.Email, courier.Password);
            return;
        }

        var email = $"{Guid.NewGuid():N}@flowbox.test";

        if (role == UserRole.Customer)
        {
            await client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterEndpoint.RegisterRequest(email, TestPassword));
        }
        else
        {
            await client.LoginAsync(SeedAdminEmail, SeedAdminPassword);
            await client.PostAsJsonAsync(
                "/api/admin/users",
                new CreateUserEndpoint.CreateUserRequest(email, TestPassword, role.ToString()));
        }

        await client.LoginAsync(email, TestPassword);
    }

    public static async Task<TestCourier> CreateCourierAsync(this HttpClient client, string name = "Test Kurye")
    {
        await client.LoginAsync(SeedAdminEmail, SeedAdminPassword);

        var email = $"{Guid.NewGuid():N}@flowbox.test";
        var response = await client.PostAsJsonAsync(
            "/api/admin/users",
            new CreateUserEndpoint.CreateUserRequest(email, TestPassword, "Courier", name, "+905551234567"));
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<CreateUserEndpoint.CreateUserResponse>();
        return new TestCourier(body!.CourierId!.Value, email, TestPassword);
    }

    public static async Task AssignShipmentAsAdminAsync(this HttpClient client, string trackingNumber, Guid courierId)
    {
        await client.LoginAsync(SeedAdminEmail, SeedAdminPassword);

        var response = await client.PatchAsJsonAsync(
            $"/api/shipments/{trackingNumber}/assign",
            new AssignShipmentEndpoint.AssignShipmentRequest(courierId));
        response.EnsureSuccessStatusCode();
    }

    public static async Task<string> LoginAsync(this HttpClient client, string email, string password)
    {
        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginEndpoint.LoginRequest(email, password));

        var body = await loginResponse.Content.ReadFromJsonAsync<LoginEndpoint.LoginResponse>();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return body.Token;
    }
}
