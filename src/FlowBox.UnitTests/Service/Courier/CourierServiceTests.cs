using FlowBox.Api.Data.Ef.Models;
using FlowBox.Api.Service.Courier;
using FluentAssertions;
using FlowBox.UnitTests.Fakes;

namespace FlowBox.UnitTests.Service.Courier;

public class CourierServiceTests
{
    private static FlowBox.Api.Data.Ef.Models.Shipment SeedShipment(
        FakeShipmentRepository repository, string trackingNumber, params (Guid CourierId, bool IsActive)[] assignments)
    {
        var shipment = new FlowBox.Api.Data.Ef.Models.Shipment
        {
            OrderId = Guid.NewGuid(),
            Origin = "Istanbul",
            Destination = "Konya",
            Weight = 3m,
            TrackingNumber = trackingNumber
        };

        foreach (var (courierId, isActive) in assignments)
        {
            shipment.Assignments.Add(new ShipmentAssignment
            {
                ShipmentId = shipment.Id,
                CourierId = courierId,
                IsActive = isActive
            });
        }

        repository.Shipments.Add(shipment);
        return shipment;
    }

    [Fact]
    public async Task GetMyShipmentsAsync_ReturnsOnlyShipmentsActivelyAssignedToTheCourier()
    {
        var shipmentRepository = new FakeShipmentRepository();
        var courierRepository = new FakeCourierRepository();
        var userId = Guid.NewGuid();
        var me = new FlowBox.Api.Data.Ef.Models.Courier { UserId = userId, Name = "Ahmet", Phone = "+905551112233" };
        var other = new FlowBox.Api.Data.Ef.Models.Courier { UserId = Guid.NewGuid(), Name = "Mehmet", Phone = "+905554445566" };
        courierRepository.Couriers.AddRange([me, other]);

        SeedShipment(shipmentRepository, "TR000001", (me.Id, true));
        SeedShipment(shipmentRepository, "TR000002", (other.Id, true));
        SeedShipment(shipmentRepository, "TR000003", (me.Id, false), (other.Id, true));
        SeedShipment(shipmentRepository, "TR000004");
        var service = new CourierService(courierRepository, shipmentRepository);

        var result = await service.GetMyShipmentsAsync(userId, 0, 20, CancellationToken.None);

        result.Select(s => s.TrackingNumber).Should().BeEquivalentTo(["TR000001"]);
    }

    [Fact]
    public async Task GetMyShipmentsAsync_ForUserWithoutCourierProfile_ReturnsEmpty()
    {
        var service = new CourierService(new FakeCourierRepository(), new FakeShipmentRepository());

        var result = await service.GetMyShipmentsAsync(Guid.NewGuid(), 0, 20, CancellationToken.None);

        result.Should().BeEmpty();
    }
}
