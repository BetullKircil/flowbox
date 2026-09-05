using FlowBox.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace FlowBox.Api.Endpoints.Shipment;

public class GetShipmentTrackingEndpoint : IEndpoint
{
    public record TrackingEventResponse(string Status, string? Location, DateTime OccurredAt);

    public record GetShipmentTrackingResponse(string TrackingNumber, List<TrackingEventResponse> Events);

    private static async Task<Results<Ok<GetShipmentTrackingResponse>, NotFound<string>>> Handle(
        string trackingNumber,
        FlowBoxDbContext db,
        CancellationToken ctx)
    {
        var shipment = await db.Shipments
            .AsNoTracking()
            .Include(s => s.TrackingEvents)
            .FirstOrDefaultAsync(s => s.TrackingNumber == trackingNumber, ctx);

        if (shipment is null)
        {
            return TypedResults.NotFound("Kargo bulunamadı.");
        }

        var events = shipment.TrackingEvents
            .OrderBy(e => e.OccurredAt)
            .Select(e => new TrackingEventResponse(e.Status.ToString(), e.Location, e.OccurredAt))
            .ToList();

        return TypedResults.Ok(new GetShipmentTrackingResponse(shipment.TrackingNumber, events));
    }

    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapGet("/api/shipments/{trackingNumber}/tracking", Handle)
            .WithOpenApi()
            .WithTags("Shipments")
            .WithSummary("Gets the full status timeline (tracking history) of a shipment.");
    }
}
