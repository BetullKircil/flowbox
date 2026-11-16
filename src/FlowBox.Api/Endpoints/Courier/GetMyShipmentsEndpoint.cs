using System.Security.Claims;
using FlowBox.Api.Extensions;
using FlowBox.Api.Service.Courier;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace FlowBox.Api.Endpoints.Courier;

public class GetMyShipmentsEndpoint : IEndpoint
{
    public record MyShipmentResponse(
        string TrackingNumber,
        string Origin,
        string Destination,
        decimal Weight,
        string Status,
        DateTime CreatedAt);

    private static async Task<Results<Ok<List<MyShipmentResponse>>, UnauthorizedHttpResult>> Handle(
        [FromQuery] int? skip,
        [FromQuery] int? take,
        ClaimsPrincipal user,
        CourierService courierService,
        CancellationToken ct)
    {
        var caller = user.ToCaller();
        if (caller is null)
        {
            return TypedResults.Unauthorized();
        }

        var shipments = await courierService.GetMyShipmentsAsync(
            caller.UserId, Math.Max(skip ?? 0, 0), Math.Clamp(take ?? 20, 1, 100), ct);

        var response = shipments
            .Select(s => new MyShipmentResponse(
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
        builder.MapGet("/api/couriers/me/shipments", Handle)
            .WithOpenApi()
            .WithTags("Couriers")
            .WithSummary("Lists the shipments currently assigned to the calling courier.")
            .RequireAuthorization(policy => policy.RequireRole("Courier"));
    }
}
