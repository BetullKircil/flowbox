using System.Net.Http.Headers;
using System.Net.Http.Json;
using FlowBox.Api.Endpoints.Auth;
using FlowBox.Api.Enums;

namespace FlowBox.IntegrationTests;

public static class TestAuthHelper
{
    public const string SeedAdminEmail = "seed-admin@flowbox.test";
    public const string SeedAdminPassword = "SeedAdminPass123!";

    private const string TestPassword = "TestPassword123!";

    public static async Task AuthenticateAsAsync(this HttpClient client, UserRole role)
    {
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
