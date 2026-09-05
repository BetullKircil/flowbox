using FlowBox.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

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
        FlowBoxDbContext db,
        CancellationToken ctx)
    {
        var shipment = await db.Shipments
            .AsNoTracking()
            .Include(s => s.Assignments)
            .ThenInclude(a => a.Courier)
            .FirstOrDefaultAsync(s => s.TrackingNumber == trackingNumber, ctx);

        if (shipment is null)
        {
            return TypedResults.NotFound("Kargo bulunamadı.");
        }

        var history = shipment.Assignments
            .OrderByDescending(a => a.AssignedAt)
            .Select(a => new HistoryRecordResponse(
                a.Courier!.Name,
                a.AssignedAt,
                a.CompletedAt,
                a.IsActive
            )).ToList();

        var response = new GetShipmentHistoryResponse(shipment.TrackingNumber, history);
        return TypedResults.Ok(response);
    }

    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapGet("/api/shipments/{trackingNumber}/history", Handle)
            .WithOpenApi()
            .WithTags("Shipments")
            .WithSummary("Gets the assignment history of a shipment.");
    }
}