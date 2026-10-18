using System.Security.Claims;
using FlowBox.Api.Extensions;
using FlowBox.Api.Service.Shipment;
using Microsoft.AspNetCore.Http.HttpResults;

namespace FlowBox.Api.Endpoints.Shipment;

public class GetShipmentEndpoint : IEndpoint
{
    public record GetShipmentResponse(
        Guid Id,
        string TrackingNumber,
        string Origin,
        string Destination,
        decimal Weight,
        string Status,
        DateTime CreatedAt);

    private static async Task<Results<Ok<GetShipmentResponse>, NotFound, UnauthorizedHttpResult>> Handle(
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

        var shipment = await shipmentService.GetByTrackingNumberAsync(trackingNumber, caller, ct);

        if (shipment is null)
        {
            return TypedResults.NotFound();
        }

        var response = new GetShipmentResponse(
            shipment.Id,
            shipment.TrackingNumber,
            shipment.Origin,
            shipment.Destination,
            shipment.Weight,
            shipment.Status.ToString(),
            shipment.CreatedAt
        );

        return TypedResults.Ok(response);
    }

    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapGet("/api/shipments/{trackingNumber}", Handle)
            .WithOpenApi()
            .WithTags("Shipments")
            .WithSummary("Get a shipment by its tracking number.")
            .RequireAuthorization();
    }
}
