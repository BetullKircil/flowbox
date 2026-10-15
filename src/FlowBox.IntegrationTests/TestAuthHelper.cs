using System.Net.Http.Headers;
using System.Net.Http.Json;
using FlowBox.Api.Endpoints.Auth;
using FlowBox.Api.Enums;

namespace FlowBox.IntegrationTests;

/// <summary>
/// Integration testlerin korumalı endpoint'lere istek atabilmesi için bir kullanıcı
/// kaydedip giriş yapar ve HttpClient'ın Authorization header'ını o kullanıcının
/// JWT'siyle doldurur. Aynı client üzerinde tekrar çağrılırsa (farklı bir rolle)
/// önceki token'ın yerini alır — bir testin içinde rol değiştirmek için kullanılabilir.
/// </summary>
public static class TestAuthHelper
{
    private const string TestPassword = "TestPassword123!";

    public static async Task AuthenticateAsAsync(this HttpClient client, UserRole role)
    {
        var email = $"{Guid.NewGuid():N}@flowbox.test";

        await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterEndpoint.RegisterRequest(email, TestPassword, role.ToString()));

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginEndpoint.LoginRequest(email, TestPassword));

        var body = await loginResponse.Content.ReadFromJsonAsync<LoginEndpoint.LoginResponse>();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
    }
}
