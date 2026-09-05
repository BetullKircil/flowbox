using FlowBox.Api.Data.Ef.Models;

namespace FlowBox.Api.Repositories.Shipment;

public interface IShipmentRepository
{
    Task<Data.Ef.Models.Shipment?> GetByTrackingNumberAsync(string trackingNumber, CancellationToken ct);
    Task<Data.Ef.Models.Shipment?> GetWithAssignmentsAsync(string trackingNumber, CancellationToken ct);
    Task<Data.Ef.Models.Shipment?> GetWithAssignmentHistoryAsync(string trackingNumber, CancellationToken ct);
    Task<Data.Ef.Models.Shipment?> GetWithTrackingEventsAsync(string trackingNumber, CancellationToken ct);
    Task<IReadOnlyList<Data.Ef.Models.Shipment>> GetPagedAsync(int skip, int take, CancellationToken ct);
    Task AddAsync(Data.Ef.Models.Shipment shipment, CancellationToken ct);
    Task AddAssignmentAsync(ShipmentAssignment assignment, CancellationToken ct);
    Task AddTrackingEventAsync(ShipmentTrackingEvent trackingEvent, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
