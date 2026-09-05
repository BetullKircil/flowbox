using FlowBox.Api.Service.Shipment;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace FlowBox.Api.Endpoints.Shipment;

public class AssignShipmentEndpoint : IEndpoint
{
    public record AssignShipmentRequest(Guid CourierId);

    public record AssignShipmentResponse(string TrackingNumber, Guid CourierId, string CourierName);

    private static async Task<Results<Ok<AssignShipmentResponse>, NotFound<string>, ValidationProblem>> Handle(
        string trackingNumber,
        [FromBody] AssignShipmentRequest request,
        ShipmentService shipmentService,
        IValidator<AssignShipmentRequest> validator,
        CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        var result = await shipmentService.AssignToCourierAsync(trackingNumber, request.CourierId, ct);

        return result switch
        {
            AssignShipmentResult.Success success => TypedResults.Ok(
                new AssignShipmentResponse(success.TrackingNumber, success.CourierId, success.CourierName)),

            AssignShipmentResult.ShipmentNotFound => TypedResults.NotFound("Kargo bulunamadı."),

            AssignShipmentResult.CourierNotFound => TypedResults.NotFound("Belirtilen kurye bulunamadı."),

            _ => throw new InvalidOperationException("Beklenmeyen sonuç türü.")
        };
    }

    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapPatch("/api/shipments/{trackingNumber}/assign", Handle)
            .WithOpenApi()
            .WithTags("Shipments")
            .WithSummary("Assigns a shipment to a specific courier.");
    }
}
