using FlowBox.Api.Data.Ef.Models;
using FlowBox.Api.Repositories.Shipment;

namespace FlowBox.UnitTests.Fakes;

public class FakeShipmentRepository : IShipmentRepository
{
    public List<Shipment> Shipments { get; } = [];

    public Task<Shipment?> GetByTrackingNumberAsync(string trackingNumber, CancellationToken ct) =>
        Task.FromResult(Shipments.FirstOrDefault(s => s.TrackingNumber == trackingNumber));

    public Task<Shipment?> GetWithAssignmentsAsync(string trackingNumber, CancellationToken ct) =>
        GetByTrackingNumberAsync(trackingNumber, ct);

    public Task<Shipment?> GetWithAssignmentHistoryAsync(string trackingNumber, CancellationToken ct) =>
        GetByTrackingNumberAsync(trackingNumber, ct);

    public Task<Shipment?> GetWithTrackingEventsAsync(string trackingNumber, CancellationToken ct) =>
        GetByTrackingNumberAsync(trackingNumber, ct);

    public Task<IReadOnlyList<Shipment>> GetPagedAsync(int skip, int take, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Shipment>>(Shipments.Skip(skip).Take(take).ToList());

    public Task AddAsync(Shipment shipment, CancellationToken ct)
    {
        Shipments.Add(shipment);
        return Task.CompletedTask;
    }

    public Task AddAssignmentAsync(ShipmentAssignment assignment, CancellationToken ct)
    {
        var shipment = Shipments.First(s => s.Id == assignment.ShipmentId);
        assignment.Shipment = shipment;
        shipment.Assignments.Add(assignment);
        return Task.CompletedTask;
    }

    public Task AddTrackingEventAsync(ShipmentTrackingEvent trackingEvent, CancellationToken ct)
    {
        var shipment = Shipments.First(s => s.Id == trackingEvent.ShipmentId);
        shipment.TrackingEvents.Add(trackingEvent);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
}
