using FlowBox.Api.Service.Shipment;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace FlowBox.Api.Endpoints.Shipment;

public class GetShipmentsEndpoint : IEndpoint
{
    public record GetShipmentsResponse(
        Guid Id,
        string TrackingNumber,
        string Origin,
        string Destination,
        decimal Weight,
        string Status,
        DateTime CreatedAt);

    private static async Task<Ok<List<GetShipmentsResponse>>> Handle(
        [FromQuery] int? skip,
        [FromQuery] int? take,
        ShipmentService shipmentService,
        CancellationToken ct)
    {
        var shipments = await shipmentService.GetPagedAsync(skip ?? 0, take ?? 20, ct);

        var response = shipments
            .Select(s => new GetShipmentsResponse(
                s.Id,
                s.TrackingNumber,
                s.Origin,
                s.Destination,
                s.Weight,
                s.Status.ToString(),
                s.CreatedAt))
            .ToList();

        return TypedResults.Ok(response);
    }

    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapGet("/api/shipments", Handle)
            .WithOpenApi()
            .WithTags("Shipments")
            .WithSummary("Gets a paginated list of all shipments.");
    }
}
