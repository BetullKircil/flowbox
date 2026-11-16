using FlowBox.Api.Endpoints.Auth;
using FlowBox.Api.Validators.Auth;
using FluentAssertions;

namespace FlowBox.UnitTests.Validators.Auth;

public class UserValidatorsTests
{
    private readonly CreateUserValidator _createUserValidator = new();
    private readonly RegisterValidator _registerValidator = new();

    [Theory]
    [InlineData("Customer")]
    [InlineData("Admin")]
    public async Task CreateUser_WithValidRole_IsValid(string role)
    {
        var result = await _createUserValidator.ValidateAsync(
            new CreateUserEndpoint.CreateUserRequest("user@flowbox.test", "SecurePass123!", role));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("SuperUser")]
    [InlineData("admin")]
    [InlineData("")]
    public async Task CreateUser_WithInvalidRole_IsInvalid(string role)
    {
        var result = await _createUserValidator.ValidateAsync(
            new CreateUserEndpoint.CreateUserRequest("user@flowbox.test", "SecurePass123!", role));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Role");
    }

    [Fact]
    public async Task CreateUser_CourierWithNameAndPhone_IsValid()
    {
        var result = await _createUserValidator.ValidateAsync(
            new CreateUserEndpoint.CreateUserRequest(
                "kurye@flowbox.test", "SecurePass123!", "Courier", "Ahmet Kurye", "+905559876543"));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null, "+905559876543", "Name")]
    [InlineData("Ah", "+905559876543", "Name")]
    [InlineData("Ahmet Kurye", null, "Phone")]
    [InlineData("Ahmet Kurye", "abc", "Phone")]
    public async Task CreateUser_CourierWithMissingOrInvalidProfile_IsInvalid(string? name, string? phone, string property)
    {
        var result = await _createUserValidator.ValidateAsync(
            new CreateUserEndpoint.CreateUserRequest(
                "kurye@flowbox.test", "SecurePass123!", "Courier", name, phone));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == property);
    }

    [Theory]
    [InlineData("Customer")]
    [InlineData("Admin")]
    public async Task CreateUser_NonCourierRole_DoesNotRequireNameOrPhone(string role)
    {
        var result = await _createUserValidator.ValidateAsync(
            new CreateUserEndpoint.CreateUserRequest("user@flowbox.test", "SecurePass123!", role));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("not-an-email", "SecurePass123!")]
    [InlineData("user@flowbox.test", "short")]
    [InlineData("", "SecurePass123!")]
    public async Task Register_WithInvalidEmailOrPassword_IsInvalid(string email, string password)
    {
        var result = await _registerValidator.ValidateAsync(
            new RegisterEndpoint.RegisterRequest(email, password));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Register_WithValidEmailAndPassword_IsValid()
    {
        var result = await _registerValidator.ValidateAsync(
            new RegisterEndpoint.RegisterRequest("user@flowbox.test", "SecurePass123!"));

        result.IsValid.Should().BeTrue();
    }
}
