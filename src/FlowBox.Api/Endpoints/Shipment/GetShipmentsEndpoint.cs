using FlowBox.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        FlowBoxDbContext db,
        CancellationToken ctx)
    {
        int skipValue = skip ?? 0;
        int takeValue = take ?? 20;

        var shipments = await db.Shipments
            .AsNoTracking()
            .OrderByDescending(s => s.CreatedAt)
            .Skip(skipValue)
            .Take(takeValue)
            .Select(s => new GetShipmentsResponse(
                s.Id,
                s.TrackingNumber,
                s.Origin,
                s.Destination,
                s.Weight,
                s.Status.ToString(),
                s.CreatedAt))
            .ToListAsync(ctx);

        return TypedResults.Ok(shipments);
    }

    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapGet("/api/shipments", Handle)
            .WithOpenApi()
            .WithTags("Shipments")
            .WithSummary("Gets a paginated list of all shipments.");
    }
}