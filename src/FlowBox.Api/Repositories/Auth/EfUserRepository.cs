using FlowBox.Api.Data.Ef;
using FlowBox.Api.Data.Ef.Models;
using Microsoft.EntityFrameworkCore;

namespace FlowBox.Api.Repositories.Auth;

public class EfUserRepository(FlowBoxDbContext db) : IUserRepository
{
    public Task<User?> GetByEmailAsync(string email, CancellationToken ct) =>
        db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task AddAsync(User user, CancellationToken ct)
    {
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
    }
}
