using FlowBox.Api.Data.Ef;
using Microsoft.EntityFrameworkCore;

namespace FlowBox.Api.Repositories.Order;

public class EfOrderRepository(FlowBoxDbContext db) : IOrderRepository
{
    public async Task AddAsync(Data.Ef.Models.Order order, CancellationToken ct)
    {
        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<Data.Ef.Models.Order>> GetByCustomerAsync(
        Guid customerId, int skip, int take, CancellationToken ct) =>
        await db.Orders
            .AsNoTracking()
            .Include(o => o.Shipment)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
}
