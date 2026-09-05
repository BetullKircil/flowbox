using FlowBox.Api.Data.Ef;
using FlowBox.Api.Data.Ef.Models;
using Microsoft.EntityFrameworkCore;

namespace FlowBox.Api.Repositories.Shipment;

public class EfShipmentRepository(FlowBoxDbContext db) : IShipmentRepository
{
    public Task<Data.Ef.Models.Shipment?> GetByTrackingNumberAsync(string trackingNumber, CancellationToken ct) =>
        db.Shipments.FirstOrDefaultAsync(s => s.TrackingNumber == trackingNumber, ct);

    public Task<Data.Ef.Models.Shipment?> GetWithAssignmentsAsync(string trackingNumber, CancellationToken ct) =>
        db.Shipments
            .Include(s => s.Assignments)
            .FirstOrDefaultAsync(s => s.TrackingNumber == trackingNumber, ct);

    public Task<Data.Ef.Models.Shipment?> GetWithAssignmentHistoryAsync(string trackingNumber, CancellationToken ct) =>
        db.Shipments
            .AsNoTracking()
            .Include(s => s.Assignments)
            .ThenInclude(a => a.Courier)
            .FirstOrDefaultAsync(s => s.TrackingNumber == trackingNumber, ct);

    public Task<Data.Ef.Models.Shipment?> GetWithTrackingEventsAsync(string trackingNumber, CancellationToken ct) =>
        db.Shipments
            .AsNoTracking()
            .Include(s => s.TrackingEvents)
            .FirstOrDefaultAsync(s => s.TrackingNumber == trackingNumber, ct);

    public async Task<IReadOnlyList<Data.Ef.Models.Shipment>> GetPagedAsync(int skip, int take, CancellationToken ct) =>
        await db.Shipments
            .AsNoTracking()
            .OrderByDescending(s => s.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

    public async Task AddAsync(Data.Ef.Models.Shipment shipment, CancellationToken ct)
    {
        db.Shipments.Add(shipment);
        await db.SaveChangesAsync(ct);
    }

    public Task AddAssignmentAsync(ShipmentAssignment assignment, CancellationToken ct)
    {
        db.ShipmentAssignments.Add(assignment);
        return Task.CompletedTask;
    }

    public Task AddTrackingEventAsync(ShipmentTrackingEvent trackingEvent, CancellationToken ct)
    {
        db.ShipmentTrackingEvents.Add(trackingEvent);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
