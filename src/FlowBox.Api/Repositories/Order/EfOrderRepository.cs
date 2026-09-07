using FlowBox.Api.Data.Ef;

namespace FlowBox.Api.Repositories.Order;

public class EfOrderRepository(FlowBoxDbContext db) : IOrderRepository
{
    public async Task AddAsync(Data.Ef.Models.Order order, CancellationToken ct)
    {
        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);
    }
}
