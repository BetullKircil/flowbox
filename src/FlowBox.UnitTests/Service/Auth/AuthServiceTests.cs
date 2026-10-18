using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FlowBox.Api.Enums;
using FlowBox.Api.Service.Auth;
using FluentAssertions;
using FlowBox.UnitTests.Fakes;
using Microsoft.Extensions.Options;

namespace FlowBox.UnitTests.Service.Auth;

public class AuthServiceTests
{
    private static readonly JwtSettings TestJwtSettings = new()
    {
        Secret = "unit-test-secret-key-must-be-at-least-32-bytes-long",
        Issuer = "FlowBox.Tests",
        Audience = "FlowBox.Tests.Clients",
        ExpiryMinutes = 60
    };

    private static AuthService CreateService(out FakeUserRepository userRepository)
    {
        userRepository = new FakeUserRepository();
        return new AuthService(userRepository, Options.Create(TestJwtSettings));
    }

    [Fact]
    public async Task RegisterAsync_NewEmail_CreatesUserWithHashedPassword()
    {
        var service = CreateService(out var userRepository);

        var result = await service.RegisterAsync("customer@flowbox.test", "SecurePass123!", UserRole.Customer, CancellationToken.None);

        result.Should().BeOfType<RegisterResult.Success>();
        userRepository.Users.Should().ContainSingle();
        var storedUser = userRepository.Users.Single();
        storedUser.Email.Should().Be("customer@flowbox.test");
        storedUser.Role.Should().Be(UserRole.Customer);
        storedUser.PasswordHash.Should().NotBe("SecurePass123!", "şifre asla düz metin saklanmamalı");
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ReturnsEmailAlreadyRegistered()
    {
        var service = CreateService(out _);
        await service.RegisterAsync("dup@flowbox.test", "SecurePass123!", UserRole.Customer, CancellationToken.None);

        var result = await service.RegisterAsync("dup@flowbox.test", "AnotherPass123!", UserRole.Courier, CancellationToken.None);

        result.Should().BeOfType<RegisterResult.EmailAlreadyRegistered>();
    }

    [Fact]
    public async Task LoginAsync_WithCorrectCredentials_ReturnsTokenCarryingRoleClaim()
    {
        var service = CreateService(out _);
        await service.RegisterAsync("courier@flowbox.test", "SecurePass123!", UserRole.Courier, CancellationToken.None);

        var result = await service.LoginAsync("courier@flowbox.test", "SecurePass123!", CancellationToken.None);

        result.Should().BeOfType<LoginResult.Success>();
        var success = (LoginResult.Success)result;
        success.Role.Should().Be("Courier");

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(success.Token);
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Courier");
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ReturnsInvalidCredentials()
    {
        var service = CreateService(out _);
        await service.RegisterAsync("admin@flowbox.test", "SecurePass123!", UserRole.Admin, CancellationToken.None);

        var result = await service.LoginAsync("admin@flowbox.test", "WrongPassword!", CancellationToken.None);

        result.Should().BeOfType<LoginResult.InvalidCredentials>();
    }

    [Fact]
    public async Task LoginAsync_WithUnknownEmail_ReturnsInvalidCredentials()
    {
        var service = CreateService(out _);

        var result = await service.LoginAsync("nobody@flowbox.test", "SomePassword!", CancellationToken.None);

        result.Should().BeOfType<LoginResult.InvalidCredentials>();
    }

    [Fact]
    public async Task EnsureAdminAsync_WhenEmailNotRegistered_CreatesAdminWhoCanLogin()
    {
        var service = CreateService(out var userRepository);

        var created = await service.EnsureAdminAsync("root@flowbox.test", "SecurePass123!", CancellationToken.None);

        created.Should().BeTrue();
        userRepository.Users.Should().ContainSingle(u => u.Email == "root@flowbox.test" && u.Role == UserRole.Admin);

        var login = await service.LoginAsync("root@flowbox.test", "SecurePass123!", CancellationToken.None);
        login.Should().BeOfType<LoginResult.Success>();
        ((LoginResult.Success)login).Role.Should().Be("Admin");
    }

    [Fact]
    public async Task EnsureAdminAsync_CalledTwice_IsIdempotent()
    {
        var service = CreateService(out var userRepository);

        await service.EnsureAdminAsync("root@flowbox.test", "SecurePass123!", CancellationToken.None);
        var secondCall = await service.EnsureAdminAsync("root@flowbox.test", "SecurePass123!", CancellationToken.None);

        secondCall.Should().BeFalse("kullanıcı zaten varken yeniden oluşturulmamalı");
        userRepository.Users.Should().ContainSingle();
    }

    [Fact]
    public async Task EnsureAdminAsync_WhenEmailBelongsToExistingUser_DoesNotChangeTheirRole()
    {
        var service = CreateService(out var userRepository);
        await service.RegisterAsync("someone@flowbox.test", "SecurePass123!", UserRole.Customer, CancellationToken.None);

        var created = await service.EnsureAdminAsync("someone@flowbox.test", "OtherPass123!", CancellationToken.None);

        created.Should().BeFalse();
        userRepository.Users.Single().Role.Should().Be(UserRole.Customer, "seed mevcut bir kullanıcıyı sessizce Admin'e yükseltmemeli");
    }
}
