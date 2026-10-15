using FlowBox.Api.Service.Auth;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace FlowBox.Api.Endpoints.Auth;

public class LoginEndpoint : IEndpoint
{
    public record LoginRequest(string Email, string Password);

    public record LoginResponse(string Token, DateTime ExpiresAt, string Role);

    private static async Task<Results<Ok<LoginResponse>, UnauthorizedHttpResult, ValidationProblem>> Handle(
        [FromBody] LoginRequest request,
        AuthService authService,
        IValidator<LoginRequest> validator,
        CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        var result = await authService.LoginAsync(request.Email, request.Password, ct);

        return result switch
        {
            LoginResult.Success success => TypedResults.Ok(
                new LoginResponse(success.Token, success.ExpiresAt, success.Role)),

            LoginResult.InvalidCredentials => TypedResults.Unauthorized(),

            _ => throw new InvalidOperationException("Beklenmeyen sonuç türü.")
        };
    }

    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapPost("/api/auth/login", Handle)
            .WithOpenApi()
            .WithTags("Auth")
            .WithSummary("Authenticates a user and returns a JWT.");
    }
}
