using FlowBox.Api.Enums;
using FlowBox.Api.Service.Shipment;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace FlowBox.Api.Endpoints.Shipment;

public class UpdateShipmentStatusEndpoint : IEndpoint
{
    private static async Task<Results<Ok<UpdateShipmentStatusResponse>, NotFound, ValidationProblem>> Handle(
        string trackingNumber,
        [FromBody] UpdateShipmentStatusRequest request,
        ShipmentService shipmentService,
        IValidator<UpdateShipmentStatusRequest> validator,
        CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        var newStatus = Enum.Parse<ShipmentStatus>(request.Status);
        var result = await shipmentService.UpdateStatusAsync(trackingNumber, newStatus, ct);

        return result switch
        {
            UpdateShipmentStatusResult.Success success => TypedResults.Ok(new UpdateShipmentStatusResponse(
                trackingNumber,
                success.OldStatus,
                success.Shipment.Status.ToString())),

            UpdateShipmentStatusResult.ShipmentNotFound => TypedResults.NotFound(),

            UpdateShipmentStatusResult.InvalidTransition invalid => TypedResults.ValidationProblem(
                new Dictionary<string, string[]> { { "Status", new[] { invalid.Message } } }),

            _ => throw new InvalidOperationException("Beklenmeyen sonuç türü.")
        };
    }

    public record UpdateShipmentStatusRequest(string Status);

    public record UpdateShipmentStatusResponse(string TrackingNumber, string OldStatus, string NewStatus);

    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapPatch("/api/shipments/{trackingNumber}/status", Handle)
            .WithOpenApi()
            .WithTags("Shipments")
            .WithSummary("Updates the current status of a shipment.");
    }
}
