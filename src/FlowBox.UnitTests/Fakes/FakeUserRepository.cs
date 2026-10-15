using FlowBox.Api.Data.Ef.Models;
using FlowBox.Api.Repositories.Auth;

namespace FlowBox.UnitTests.Fakes;

public class FakeUserRepository : IUserRepository
{
    public List<User> Users { get; } = [];

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct) =>
        Task.FromResult(Users.FirstOrDefault(u => u.Email == email));

    public Task AddAsync(User user, CancellationToken ct)
    {
        Users.Add(user);
        return Task.CompletedTask;
    }
}
