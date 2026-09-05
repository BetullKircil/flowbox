using FlowBox.Api.Service.Courier;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace FlowBox.Api.Endpoints.Courier;

public class CreateCourierEndpoint : IEndpoint
{
    public record CreateCourierRequest(string Name, string Phone);

    public record CreateCourierResponse(Guid Id, string Name, string Phone);

    private static async Task<Results<Created<CreateCourierResponse>, ValidationProblem>> Handle(
        [FromBody] CreateCourierRequest request,
        CourierService courierService,
        IValidator<CreateCourierRequest> validator,
        CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        var courier = await courierService.CreateAsync(request.Name, request.Phone, ct);

        var response = new CreateCourierResponse(courier.Id, courier.Name, courier.Phone);
        return TypedResults.Created($"/api/couriers/{courier.Id}", response);
    }

    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapPost("/api/couriers", Handle)
            .WithOpenApi()
            .WithTags("Couriers")
            .WithSummary("Creates a new courier.");
    }
}
