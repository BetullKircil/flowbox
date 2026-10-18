using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FlowBox.Api.Data.Ef.Models;
using FlowBox.Api.Enums;
using FlowBox.Api.Repositories.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FlowBox.Api.Service.Auth;

/// <summary>
/// Kullanıcı kaydı, giriş doğrulaması ve JWT üretimi. Şifreler asla düz metin
/// tutulmuyor — Microsoft'un PasswordHasher&lt;T&gt; sınıfıyla (PBKDF2 tabanlı,
/// production-grade) hash'leniyor. ASP.NET Core Identity'nin tamamını değil,
/// sadece bu tek, iyi test edilmiş parçasını kullanıyoruz.
/// </summary>
public class AuthService(IUserRepository userRepository, IOptions<JwtSettings> jwtOptions) : IService
{
    private readonly PasswordHasher<User> _passwordHasher = new();
    private readonly JwtSettings _jwtSettings = jwtOptions.Value;

    public async Task<RegisterResult> RegisterAsync(
        string email, string password, UserRole role, CancellationToken ct)
    {
        var existing = await userRepository.GetByEmailAsync(email, ct);
        if (existing is not null)
        {
            return new RegisterResult.EmailAlreadyRegistered();
        }

        var user = new User
        {
            Email = email,
            Role = role
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, password);

        await userRepository.AddAsync(user, ct);

        return new RegisterResult.Success(user.Id, user.Email, user.Role.ToString());
    }

    public async Task<bool> EnsureAdminAsync(string email, string password, CancellationToken ct)
    {
        var result = await RegisterAsync(email, password, UserRole.Admin, ct);
        return result is RegisterResult.Success;
    }

    public async Task<LoginResult> LoginAsync(string email, string password, CancellationToken ct)
    {
        var user = await userRepository.GetByEmailAsync(email, ct);
        if (user is null)
        {
            // Kullanıcı yok mu, şifre mi yanlış — aynı mesajı döndürüyoruz.
            // "Bu e-posta kayıtlı mı" bilgisini sızdırmamak için kasıtlı.
            return new LoginResult.InvalidCredentials();
        }

        var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return new LoginResult.InvalidCredentials();
        }

        var (token, expiresAt) = GenerateToken(user);
        return new LoginResult.Success(token, expiresAt, user.Role.ToString());
    }

    private (string Token, DateTime ExpiresAt) GenerateToken(User user)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes);

        Claim[] claims =
        [
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            // ClaimTypes.Role kullanıyoruz çünkü ASP.NET Core'un varsayılan rol
            // eşleştirmesi (RequireRole / [Authorize(Roles=...)]) bunu bekliyor.
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        ];

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
