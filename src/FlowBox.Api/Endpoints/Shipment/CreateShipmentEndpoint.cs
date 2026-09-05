using FlowBox.Api.Service.Shipment;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace FlowBox.Api.Endpoints.Shipment;

public class CreateShipmentEndpoint : IEndpoint
{
    private static async Task<Results<Created<CreateShipmentResponse>, ValidationProblem>> Handle(
        [FromBody] CreateShipmentRequest request,
        ShipmentService shipmentService,
        IValidator<CreateShipmentRequest> validator,
        CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        var shipment = await shipmentService.CreateAsync(request.Origin, request.Destination, request.Weight, ct);

        var response = new CreateShipmentResponse(shipment.Id, shipment.TrackingNumber, shipment.Status.ToString());
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
