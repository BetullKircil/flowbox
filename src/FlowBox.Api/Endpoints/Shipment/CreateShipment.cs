using FlowBox.Api.Data;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace FlowBox.Api.Endpoints.Shipment;

public class CreateShipmentEndpoint : IEndpoint
{
    private static async Task<Results<Created<CreateShipmentResponse>, ValidationProblem>> Handle(
        [FromBody] CreateShipmentRequest request,
        FlowBoxDbContext db,
        CancellationToken ctx,
        IValidator<CreateShipmentRequest> validator)
    {
        var validationResult = await validator.ValidateAsync(request, ctx);

        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        var shipment = new FlowBox.Api.Models.Shipment
        {
            Origin = request.Origin,
            Destination = request.Destination,
            Weight = request.Weight
        };

        shipment.TrackingNumber = $"TR{Random.Shared.Next(100000, 999999)}";

        db.Shipments.Add(shipment);
        await db.SaveChangesAsync(ctx);

        var response = new CreateShipmentResponse(
            shipment.Id,
            shipment.TrackingNumber,
            shipment.Status.ToString());

        return TypedResults.Created($"/api/shipments/{shipment.TrackingNumber}", response);
    }

    public record CreateShipmentRequest(string Origin, string Destination, decimal Weight);

    public record CreateShipmentResponse(Guid Id, string TrackingNumber, string Status);

    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapPost("/api/shipments", Handle)
            .WithOpenApi()
            .WithTags("Shipments")
            .WithSummary("Creates a new shipment in the system.");
    }
}