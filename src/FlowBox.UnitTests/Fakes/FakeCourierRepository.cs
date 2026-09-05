using FlowBox.Api.Data.Ef.Models;
using FlowBox.Api.Repositories.Courier;

namespace FlowBox.UnitTests.Fakes;

public class FakeCourierRepository : ICourierRepository
{
    public List<Courier> Couriers { get; } = [];

    public Task<Courier?> GetByIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Couriers.FirstOrDefault(c => c.Id == id));

    public Task AddAsync(Courier courier, CancellationToken ct)
    {
        Couriers.Add(courier);
        return Task.CompletedTask;
    }
}
