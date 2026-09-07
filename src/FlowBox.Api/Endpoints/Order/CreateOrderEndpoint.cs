using FlowBox.Api.Service.Order;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace FlowBox.Api.Endpoints.Order;

/// <summary>
/// Müşterinin kargo gönderme talebinin giriş noktası. Order oluşturulur ve
/// bugün için hemen bir Shipment'e dönüştürülür; müşteri geriye kargosunu
/// takip edeceği tracking number'ı alır.
/// </summary>
public class CreateOrderEndpoint : IEndpoint
{
    public record CreateOrderRequest(string Origin, string Destination, decimal Weight);

    public record CreateOrderResponse(Guid OrderId, Guid ShipmentId, string TrackingNumber, string Status);

    private static async Task<Results<Created<CreateOrderResponse>, ValidationProblem>> Handle(
        [FromBody] CreateOrderRequest request,
        OrderService orderService,
        IValidator<CreateOrderRequest> validator,
        CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        var order = await orderService.CreateAsync(request.Origin, request.Destination, request.Weight, ct);
        var shipment = order.Shipment!;

        var response = new CreateOrderResponse(order.Id, shipment.Id, shipment.TrackingNumber, shipment.Status.ToString());

        return TypedResults.Created($"/api/shipments/{shipment.TrackingNumber}", response);
    }

    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapPost("/api/orders", Handle)
            .WithOpenApi()
            .WithTags("Orders")
            .WithSummary("Places a new order and creates its shipment.");
    }
}
