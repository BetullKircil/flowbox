using FlowBox.Api.Enums;
using FlowBox.Api.Service.Order;
using FluentAssertions;
using FlowBox.UnitTests.Fakes;

namespace FlowBox.UnitTests.Service.Order;

public class OrderServiceTests
{
    [Fact]
    public async Task CreateAsync_CreatesOrderWithLinkedShipmentAndInitialTrackingEvent()
    {
        var orderRepository = new FakeOrderRepository();
        var service = new OrderService(orderRepository);

        var order = await service.CreateAsync("Istanbul", "Konya", 3m, CancellationToken.None);

        orderRepository.Orders.Should().ContainSingle(o => o.Id == order.Id);
        order.Shipment.Should().NotBeNull();
        order.Shipment!.OrderId.Should().Be(order.Id);
        order.Shipment.TrackingNumber.Should().StartWith("TR");
        order.Shipment.TrackingEvents.Should().ContainSingle(e => e.Status == ShipmentStatus.Created);
    }
}
