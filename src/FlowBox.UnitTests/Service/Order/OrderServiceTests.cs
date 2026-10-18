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

        var order = await service.CreateAsync(Guid.NewGuid(), "Istanbul", "Konya", 3m, CancellationToken.None);

        orderRepository.Orders.Should().ContainSingle(o => o.Id == order.Id);
        order.Shipment.Should().NotBeNull();
        order.Shipment!.OrderId.Should().Be(order.Id);
        order.Shipment.TrackingNumber.Should().StartWith("TR");
        order.Shipment.TrackingEvents.Should().ContainSingle(e => e.Status == ShipmentStatus.Created);
    }

    [Fact]
    public async Task CreateAsync_AssignsTheOrderToTheGivenCustomer()
    {
        var customerId = Guid.NewGuid();
        var service = new OrderService(new FakeOrderRepository());

        var order = await service.CreateAsync(customerId, "Istanbul", "Konya", 3m, CancellationToken.None);

        order.CustomerId.Should().Be(customerId);
    }

    [Fact]
    public async Task GetMyOrdersAsync_ReturnsOnlyTheGivenCustomersOrders()
    {
        var service = new OrderService(new FakeOrderRepository());
        var me = Guid.NewGuid();
        var someoneElse = Guid.NewGuid();
        await service.CreateAsync(me, "Istanbul", "Konya", 1m, CancellationToken.None);
        await service.CreateAsync(me, "Ankara", "Izmir", 2m, CancellationToken.None);
        await service.CreateAsync(someoneElse, "Bursa", "Adana", 3m, CancellationToken.None);

        var result = await service.GetMyOrdersAsync(me, 0, 20, CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(o => o.CustomerId == me);
    }
}
