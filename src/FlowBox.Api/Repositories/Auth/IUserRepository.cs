using FlowBox.Api.Data.Ef.Models;

namespace FlowBox.Api.Repositories.Auth;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);
    Task AddAsync(User user, CancellationToken ct);
}
