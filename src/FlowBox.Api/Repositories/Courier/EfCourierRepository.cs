using FlowBox.Api.Data.Ef;
using Microsoft.EntityFrameworkCore;

namespace FlowBox.Api.Repositories.Courier;

public class EfCourierRepository(FlowBoxDbContext db) : ICourierRepository
{
    public Task<Data.Ef.Models.Courier?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Couriers.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<Data.Ef.Models.Courier?> GetByUserIdAsync(Guid userId, CancellationToken ct) =>
        db.Couriers.AsNoTracking().FirstOrDefaultAsync(c => c.UserId == userId, ct);
}
