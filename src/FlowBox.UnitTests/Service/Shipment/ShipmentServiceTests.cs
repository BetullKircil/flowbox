using FlowBox.Api.Data.Ef.Models;
using FlowBox.Api.Enums;
using FlowBox.Api.Service.Shipment;
using FluentAssertions;
using FlowBox.UnitTests.Fakes;

namespace FlowBox.UnitTests.Service.Shipment;

/// <summary>
/// Bu testler ShipmentService'in iş kurallarını, gerçek bir veritabanı olmadan
/// (FakeShipmentRepository/FakeCourierRepository ile) doğrular. Shipment artık
/// Order üzerinden oluşturulduğu için (bkz. OrderServiceTests), buradaki testler
/// repository'ye doğrudan bir Shipment seed edip ondan sonraki adımları (statü
/// geçişi, kurye atama, geçmiş/tracking) doğruluyor.
/// </summary>
public class ShipmentServiceTests
{
    private static ShipmentService CreateService(
        out FakeShipmentRepository shipmentRepository,
        out FakeCourierRepository courierRepository)
    {
        shipmentRepository = new FakeShipmentRepository();
        courierRepository = new FakeCourierRepository();
        return new ShipmentService(shipmentRepository, courierRepository);
    }

    private static FlowBox.Api.Data.Ef.Models.Shipment SeedShipment(FakeShipmentRepository repository)
    {
        var shipment = new FlowBox.Api.Data.Ef.Models.Shipment
        {
            OrderId = Guid.NewGuid(),
            Origin = "Istanbul",
            Destination = "Konya",
            Weight = 3m,
            TrackingNumber = $"TR{Random.Shared.Next(100000, 999999)}"
        };

        shipment.TrackingEvents.Add(new ShipmentTrackingEvent
        {
            ShipmentId = shipment.Id,
            Status = shipment.Status,
            Location = shipment.Origin
        });

        repository.Shipments.Add(shipment);
        return shipment;
    }

    [Fact]
    public async Task UpdateStatusAsync_ForUnknownTrackingNumber_ReturnsShipmentNotFound()
    {
        var service = CreateService(out _, out _);

        var result = await service.UpdateStatusAsync("TR000000", ShipmentStatus.PickedUp, CancellationToken.None);

        result.Should().BeOfType<UpdateShipmentStatusResult.ShipmentNotFound>();
    }

    [Fact]
    public async Task UpdateStatusAsync_SkippingAheadInPipeline_ReturnsInvalidTransition()
    {
        var service = CreateService(out var shipmentRepository, out _);
        var shipment = SeedShipment(shipmentRepository);

        var result = await service.UpdateStatusAsync(shipment.TrackingNumber, ShipmentStatus.Delivered, CancellationToken.None);

        result.Should().BeOfType<UpdateShipmentStatusResult.InvalidTransition>();
        shipment.Status.Should().Be(ShipmentStatus.Created, "geçersiz geçişte statü değişmemeli");
    }

    [Fact]
    public async Task UpdateStatusAsync_WithValidTransition_UpdatesStatusAndAppendsTrackingEvent()
    {
        var service = CreateService(out var shipmentRepository, out _);
        var shipment = SeedShipment(shipmentRepository);

        var result = await service.UpdateStatusAsync(shipment.TrackingNumber, ShipmentStatus.PickedUp, CancellationToken.None);

        result.Should().BeOfType<UpdateShipmentStatusResult.Success>();
        shipment.Status.Should().Be(ShipmentStatus.PickedUp);
        shipment.TrackingEvents.Should().HaveCount(2); // Created + PickedUp
    }

    [Fact]
    public async Task AssignToCourierAsync_WithUnknownCourier_ReturnsCourierNotFound()
    {
        var service = CreateService(out var shipmentRepository, out _);
        var shipment = SeedShipment(shipmentRepository);

        var result = await service.AssignToCourierAsync(shipment.TrackingNumber, Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<AssignShipmentResult.CourierNotFound>();
    }

    [Fact]
    public async Task AssignToCourierAsync_ReassigningShipment_DeactivatesPreviousAssignment()
    {
        var service = CreateService(out var shipmentRepository, out var courierRepository);
        var shipment = SeedShipment(shipmentRepository);
        var courier1 = new FlowBox.Api.Data.Ef.Models.Courier { Name = "Ahmet", Phone = "+905551112233" };
        var courier2 = new FlowBox.Api.Data.Ef.Models.Courier { Name = "Mehmet", Phone = "+905554445566" };
        courierRepository.Couriers.Add(courier1);
        courierRepository.Couriers.Add(courier2);

        await service.AssignToCourierAsync(shipment.TrackingNumber, courier1.Id, CancellationToken.None);
        var secondResult = await service.AssignToCourierAsync(shipment.TrackingNumber, courier2.Id, CancellationToken.None);

        secondResult.Should().BeOfType<AssignShipmentResult.Success>();
        shipment.Assignments.Should().HaveCount(2);
        shipment.Assignments.Single(a => a.CourierId == courier1.Id).IsActive.Should().BeFalse();
        shipment.Assignments.Single(a => a.CourierId == courier2.Id).IsActive.Should().BeTrue();
    }
}
