using FlowBox.Api.Service.Shipment;
using Microsoft.AspNetCore.Http.HttpResults;

namespace FlowBox.Api.Endpoints.Shipment;

public class GetShipmentHistoryEndpoint : IEndpoint
{
    public record HistoryRecordResponse(
        string CourierName,
        DateTime AssignedAt,
        DateTime? CompletedAt,
        bool IsActive);

    public record GetShipmentHistoryResponse(
        string TrackingNumber,
        List<HistoryRecordResponse> History);

    private static async Task<Results<Ok<GetShipmentHistoryResponse>, NotFound<string>>> Handle(
        string trackingNumber,
        ShipmentService shipmentService,
        CancellationToken ct)
    {
        var result = await shipmentService.GetAssignmentHistoryAsync(trackingNumber, ct);

        return result switch
        {
            ShipmentHistoryResult.Found found => TypedResults.Ok(new GetShipmentHistoryResponse(
                found.TrackingNumber,
                found.History.Select(a => new HistoryRecordResponse(
                    a.Courier!.Name,
                    a.AssignedAt,
                    a.CompletedAt,
                    a.IsActive
                )).ToList())),

            ShipmentHistoryResult.NotFound => TypedResults.NotFound("Kargo bulunamadı."),

            _ => throw new InvalidOperationException("Beklenmeyen sonuç türü.")
        };
    }

    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapGet("/api/shipments/{trackingNumber}/history", Handle)
            .WithOpenApi()
            .WithTags("Shipments")
            .WithSummary("Gets the assignment history of a shipment.");
    }
}
