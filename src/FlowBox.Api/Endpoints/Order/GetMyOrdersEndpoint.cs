using System.Security.Claims;
using FlowBox.Api.Extensions;
using FlowBox.Api.Service.Order;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace FlowBox.Api.Endpoints.Order;

public class GetMyOrdersEndpoint : IEndpoint
{
    public record MyOrderResponse(
        Guid OrderId,
        string? TrackingNumber,
        string Origin,
        string Destination,
        decimal Weight,
        string Status,
        DateTime CreatedAt);

    private static async Task<Results<Ok<List<MyOrderResponse>>, UnauthorizedHttpResult>> Handle(
        [FromQuery] int? skip,
        [FromQuery] int? take,
        ClaimsPrincipal user,
        OrderService orderService,
        CancellationToken ct)
    {
        var caller = user.ToCaller();
        if (caller is null)
        {
            return TypedResults.Unauthorized();
        }

        var orders = await orderService.GetMyOrdersAsync(
            caller.UserId, Math.Max(skip ?? 0, 0), Math.Clamp(take ?? 20, 1, 100), ct);

        var response = orders
            .Select(o => new MyOrderResponse(
                o.Id,
                o.Shipment?.TrackingNumber,
                o.Origin,
                o.Destination,
                o.Weight,
                o.Shipment?.Status.ToString() ?? o.Status.ToString(),
                o.CreatedAt))
            .ToList();

        return TypedResults.Ok(response);
    }

    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapGet("/api/orders/mine", Handle)
            .WithOpenApi()
            .WithTags("Orders")
            .WithSummary("Lists the orders (and their shipment status) placed by the calling user.")
            .RequireAuthorization(policy => policy.RequireRole("Customer", "Admin"));
    }
}
