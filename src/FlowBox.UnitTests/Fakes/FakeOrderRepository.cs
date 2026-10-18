using FlowBox.Api.Data.Ef.Models;
using FlowBox.Api.Repositories.Order;

namespace FlowBox.UnitTests.Fakes;

public class FakeOrderRepository : IOrderRepository
{
    public List<Order> Orders { get; } = [];

    public Task AddAsync(Order order, CancellationToken ct)
    {
        Orders.Add(order);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Order>> GetByCustomerAsync(Guid customerId, int skip, int take, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Order>>(Orders
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToList());
}
