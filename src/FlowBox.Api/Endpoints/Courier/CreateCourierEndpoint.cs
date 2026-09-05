using FlowBox.Api.Data;
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
        FlowBoxDbContext db,
        IValidator<CreateCourierRequest> validator,
        CancellationToken ctx)
    {
        var validationResult = await validator.ValidateAsync(request, ctx);
        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        var courier = new FlowBox.Api.Models.Courier
        {
            Name = request.Name,
            Phone = request.Phone
        };

        db.Couriers.Add(courier);
        await db.SaveChangesAsync(ctx);

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