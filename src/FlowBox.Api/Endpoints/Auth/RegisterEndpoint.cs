using FlowBox.Api.Enums;
using FlowBox.Api.Service.Auth;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace FlowBox.Api.Endpoints.Auth;

public class RegisterEndpoint : IEndpoint
{
    public record RegisterRequest(string Email, string Password);

    public record RegisterResponse(Guid Id, string Email, string Role);

    private static async Task<Results<Created<RegisterResponse>, ValidationProblem>> Handle(
        [FromBody] RegisterRequest request,
        AuthService authService,
        IValidator<RegisterRequest> validator,
        CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        var result = await authService.RegisterAsync(request.Email, request.Password, UserRole.Customer, ct);

        return result switch
        {
            RegisterResult.Success success => TypedResults.Created(
                $"/api/auth/users/{success.UserId}",
                new RegisterResponse(success.UserId, success.Email, success.Role)),

            RegisterResult.EmailAlreadyRegistered => TypedResults.ValidationProblem(
                new Dictionary<string, string[]> { { "Email", new[] { "Bu e-posta adresi zaten kayıtlı." } } }),

            _ => throw new InvalidOperationException("Beklenmeyen sonuç türü.")
        };
    }

    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapPost("/api/auth/register", Handle)
            .WithOpenApi()
            .WithTags("Auth")
            .WithSummary("Registers a new Customer account.");
    }
}
