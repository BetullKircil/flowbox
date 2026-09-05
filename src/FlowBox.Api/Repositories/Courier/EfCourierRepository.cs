using FlowBox.Api.Data.Ef;
using Microsoft.EntityFrameworkCore;

namespace FlowBox.Api.Repositories.Courier;

public class EfCourierRepository(FlowBoxDbContext db) : ICourierRepository
{
    public Task<Data.Ef.Models.Courier?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Couriers.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task AddAsync(Data.Ef.Models.Courier courier, CancellationToken ct)
    {
        db.Couriers.Add(courier);
        await db.SaveChangesAsync(ct);
    }
}
