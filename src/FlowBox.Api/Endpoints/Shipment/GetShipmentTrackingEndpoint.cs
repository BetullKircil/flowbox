using System.Security.Claims;
using FlowBox.Api.Extensions;
using FlowBox.Api.Service.Shipment;
using Microsoft.AspNetCore.Http.HttpResults;

namespace FlowBox.Api.Endpoints.Shipment;

/// <summary>
/// Customer App'teki "kargom nerede" zaman çizelgesinin backend karşılığı:
/// bir kargonun geçtiği tüm statüleri kronolojik sırayla döner.
/// </summary>
public class GetShipmentTrackingEndpoint : IEndpoint
{
    public record TrackingEventResponse(string Status, string? Location, DateTime OccurredAt);

    public record GetShipmentTrackingResponse(string TrackingNumber, List<TrackingEventResponse> Events);

    private static async Task<Results<Ok<GetShipmentTrackingResponse>, NotFound<string>, UnauthorizedHttpResult>> Handle(
        string trackingNumber,
        ClaimsPrincipal user,
        ShipmentService shipmentService,
        CancellationToken ct)
    {
        var caller = user.ToCaller();
        if (caller is null)
        {
            return TypedResults.Unauthorized();
        }

        var result = await shipmentService.GetTrackingAsync(trackingNumber, caller, ct);

        return result switch
        {
            ShipmentTrackingResult.Found found => TypedResults.Ok(new GetShipmentTrackingResponse(
                found.TrackingNumber,
                found.Events.Select(e => new TrackingEventResponse(
                    e.Status.ToString(), e.Location, e.OccurredAt)).ToList())),

            ShipmentTrackingResult.NotFound => TypedResults.NotFound("Kargo bulunamadı."),

            _ => throw new InvalidOperationException("Beklenmeyen sonuç türü.")
        };
    }

    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapGet("/api/shipments/{trackingNumber}/tracking", Handle)
            .WithOpenApi()
            .WithTags("Shipments")
            .WithSummary("Gets the full status timeline (tracking history) of a shipment.")
            .RequireAuthorization();
    }
}
