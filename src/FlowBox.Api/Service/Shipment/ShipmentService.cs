using FlowBox.Api.Data.Ef.Models;
using FlowBox.Api.Domain.Shipment;
using FlowBox.Api.Enums;
using FlowBox.Api.Repositories.Courier;
using FlowBox.Api.Repositories.Shipment;

namespace FlowBox.Api.Service.Shipment;

public class ShipmentService(
    IShipmentRepository shipmentRepository,
    ICourierRepository courierRepository) : IService
{
    public async Task<Data.Ef.Models.Shipment?> GetByTrackingNumberAsync(
        string trackingNumber, Caller caller, CancellationToken ct)
    {
        var shipment = await shipmentRepository.GetByTrackingNumberAsync(trackingNumber, ct);
        return shipment is not null && await CanAccessAsync(shipment, caller, ct) ? shipment : null;
    }

    private async Task<bool> CanAccessAsync(Data.Ef.Models.Shipment shipment, Caller caller, CancellationToken ct)
    {
        switch (caller.Role)
        {
            case UserRole.Admin:
                return true;

            case UserRole.Customer:
                return shipment.Order?.CustomerId == caller.UserId;

            case UserRole.Courier:
                var courier = await courierRepository.GetByUserIdAsync(caller.UserId, ct);
                return courier is not null
                       && shipment.Assignments.Any(a => a.IsActive && a.CourierId == courier.Id);

            default:
                return false;
        }
    }

    public Task<IReadOnlyList<Data.Ef.Models.Shipment>> GetPagedAsync(int skip, int take, CancellationToken ct) =>
        shipmentRepository.GetPagedAsync(skip, take, ct);

    public async Task<UpdateShipmentStatusResult> UpdateStatusAsync(
        string trackingNumber, ShipmentStatus newStatus, Caller caller, CancellationToken ct)
    {
        var shipment = await shipmentRepository.GetByTrackingNumberAsync(trackingNumber, ct);
        if (shipment is null || !await CanAccessAsync(shipment, caller, ct))
        {
            return new UpdateShipmentStatusResult.ShipmentNotFound();
        }

        if (!ShipmentStatusTransitions.IsValidTransition(shipment.Status, newStatus))
        {
            var validNext = ShipmentStatusTransitions.GetValidNextStatuses(shipment.Status);
            var message = validNext.Count == 0
                ? $"Kargo şu anda '{shipment.Status}' durumunda olduğu için statüsü artık güncellenemez."
                : $"'{shipment.Status}' durumundan '{newStatus}' durumuna geçilemez. Geçerli sonraki durumlar: {string.Join(", ", validNext)}.";

            return new UpdateShipmentStatusResult.InvalidTransition(message);
        }

        var oldStatus = shipment.Status;
        shipment.Status = newStatus;

        await shipmentRepository.AddTrackingEventAsync(
            new ShipmentTrackingEvent { ShipmentId = shipment.Id, Status = newStatus }, ct);

        await shipmentRepository.SaveChangesAsync(ct);

        return new UpdateShipmentStatusResult.Success(shipment, oldStatus.ToString());
    }

    public async Task<AssignShipmentResult> AssignToCourierAsync(
        string trackingNumber, Guid courierId, CancellationToken ct)
    {
        var shipment = await shipmentRepository.GetWithAssignmentsAsync(trackingNumber, ct);
        if (shipment is null)
        {
            return new AssignShipmentResult.ShipmentNotFound();
        }
        
        var courier = await courierRepository.GetByIdAsync(courierId, ct);
        if (courier is null)
        {
            return new AssignShipmentResult.CourierNotFound();
        }

        var activeAssignment = shipment.Assignments.FirstOrDefault(a => a.IsActive);
        if (activeAssignment is not null)
        {
            activeAssignment.IsActive = false;
            activeAssignment.CompletedAt = DateTime.UtcNow;
        }

        var newAssignment = new ShipmentAssignment
        {
            ShipmentId = shipment.Id,
            CourierId = courier.Id
        };

        await shipmentRepository.AddAssignmentAsync(newAssignment, ct);
        await shipmentRepository.SaveChangesAsync(ct);

        return new AssignShipmentResult.Success(shipment.TrackingNumber, courier.Id, courier.Name);
    }

    public async Task<ShipmentHistoryResult> GetAssignmentHistoryAsync(
        string trackingNumber, Caller caller, CancellationToken ct)
    {
        var shipment = await shipmentRepository.GetWithAssignmentHistoryAsync(trackingNumber, ct);
        if (shipment is null || !await CanAccessAsync(shipment, caller, ct))
        {
            return new ShipmentHistoryResult.NotFound();
        }

        var history = shipment.Assignments
            .OrderByDescending(a => a.AssignedAt)
            .ToList();

        return new ShipmentHistoryResult.Found(shipment.TrackingNumber, history);
    }

    public async Task<ShipmentTrackingResult> GetTrackingAsync(
        string trackingNumber, Caller caller, CancellationToken ct)
    {
        var shipment = await shipmentRepository.GetWithTrackingEventsAsync(trackingNumber, ct);
        if (shipment is null || !await CanAccessAsync(shipment, caller, ct))
        {
            return new ShipmentTrackingResult.NotFound();
        }

        var events = shipment.TrackingEvents
            .OrderBy(e => e.OccurredAt)
            .ToList();

        return new ShipmentTrackingResult.Found(shipment.TrackingNumber, events);
    }
}
