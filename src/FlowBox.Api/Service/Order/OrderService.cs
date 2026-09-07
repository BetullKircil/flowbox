using FlowBox.Api.Data.Ef.Models;
using FlowBox.Api.Repositories.Order;

namespace FlowBox.Api.Service.Order;

/// <summary>
/// Müşterinin "gönder" dediği an burada başlar: bir Order oluşturulur ve
/// bugün için senkron olarak ondan bir Shipment üretilir (tracking number
/// burada atanır). İleride ödeme onayı gibi bir adım araya girdiğinde bu iki
/// işlem ayrılacak — Order'ı Shipment'ten ayrı bir entity yapmamızın sebebi bu.
/// </summary>
public class OrderService(IOrderRepository orderRepository) : IService
{
    public async Task<Data.Ef.Models.Order> CreateAsync(
        string origin, string destination, decimal weight, CancellationToken ct)
    {
        var order = new Data.Ef.Models.Order
        {
            Origin = origin,
            Destination = destination,
            Weight = weight
        };

        var shipment = new Data.Ef.Models.Shipment
        {
            OrderId = order.Id,
            Origin = origin,
            Destination = destination,
            Weight = weight,
            TrackingNumber = $"TR{Random.Shared.Next(100000, 999999)}"
        };

        shipment.TrackingEvents.Add(new ShipmentTrackingEvent
        {
            ShipmentId = shipment.Id,
            Status = shipment.Status,
            Location = shipment.Origin
        });

        order.Shipment = shipment;

        await orderRepository.AddAsync(order, ct);
        return order;
    }
}
