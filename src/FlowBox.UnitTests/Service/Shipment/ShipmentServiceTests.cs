using FlowBox.Api.Enums;
using FlowBox.Api.Service.Shipment;
using FluentAssertions;
using FlowBox.UnitTests.Fakes;

namespace FlowBox.UnitTests.Service.Shipment;

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

    [Fact]
    public async Task CreateAsync_AddsShipmentWithInitialCreatedTrackingEvent()
    {
        var service = CreateService(out var shipmentRepository, out _);

        var shipment = await service.CreateAsync("Istanbul", "Konya", 3m, CancellationToken.None);

        shipmentRepository.Shipments.Should().ContainSingle(s => s.Id == shipment.Id);
        shipment.TrackingEvents.Should().ContainSingle(e => e.Status == ShipmentStatus.Created);
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
        var service = CreateService(out _, out _);
        var shipment = await service.CreateAsync("Istanbul", "Konya", 3m, CancellationToken.None);

        var result = await service.UpdateStatusAsync(shipment.TrackingNumber, ShipmentStatus.Delivered, CancellationToken.None);

        result.Should().BeOfType<UpdateShipmentStatusResult.InvalidTransition>();
        shipment.Status.Should().Be(ShipmentStatus.Created, "geçersiz geçişte statü değişmemeli");
    }

    [Fact]
    public async Task UpdateStatusAsync_WithValidTransition_UpdatesStatusAndAppendsTrackingEvent()
    {
        var service = CreateService(out _, out _);
        var shipment = await service.CreateAsync("Istanbul", "Konya", 3m, CancellationToken.None);

        var result = await service.UpdateStatusAsync(shipment.TrackingNumber, ShipmentStatus.PickedUp, CancellationToken.None);

        result.Should().BeOfType<UpdateShipmentStatusResult.Success>();
        shipment.Status.Should().Be(ShipmentStatus.PickedUp);
        shipment.TrackingEvents.Should().HaveCount(2); // Created + PickedUp
    }

    [Fact]
    public async Task AssignToCourierAsync_WithUnknownCourier_ReturnsCourierNotFound()
    {
        var service = CreateService(out _, out _);
        var shipment = await service.CreateAsync("Istanbul", "Konya", 3m, CancellationToken.None);

        var result = await service.AssignToCourierAsync(shipment.TrackingNumber, Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<AssignShipmentResult.CourierNotFound>();
    }

    [Fact]
    public async Task AssignToCourierAsync_ReassigningShipment_DeactivatesPreviousAssignment()
    {
        var service = CreateService(out _, out var courierRepository);
        var shipment = await service.CreateAsync("Istanbul", "Konya", 3m, CancellationToken.None);
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