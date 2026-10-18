using FlowBox.Api.Enums;
using FlowBox.Api.Service.Auth;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace FlowBox.Api.Endpoints.Auth;

public class CreateUserEndpoint : IEndpoint
{
    public record CreateUserRequest(string Email, string Password, string Role);

    public record CreateUserResponse(Guid Id, string Email, string Role);

    private static async Task<Results<Created<CreateUserResponse>, ValidationProblem>> Handle(
        [FromBody] CreateUserRequest request,
        AuthService authService,
        IValidator<CreateUserRequest> validator,
        CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        var role = Enum.Parse<UserRole>(request.Role);
        var result = await authService.RegisterAsync(request.Email, request.Password, role, ct);

        return result switch
        {
            RegisterResult.Success success => TypedResults.Created(
                $"/api/auth/users/{success.UserId}",
                new CreateUserResponse(success.UserId, success.Email, success.Role)),

            RegisterResult.EmailAlreadyRegistered => TypedResults.ValidationProblem(
                new Dictionary<string, string[]> { { "Email", new[] { "Bu e-posta adresi zaten kayıtlı." } } }),

            _ => throw new InvalidOperationException("Beklenmeyen sonuç türü.")
        };
    }

    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapPost("/api/admin/users", Handle)
            .WithOpenApi()
            .WithTags("Auth")
            .WithSummary("Creates a user with a chosen role (Customer, Courier or Admin). Admin only.")
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}
