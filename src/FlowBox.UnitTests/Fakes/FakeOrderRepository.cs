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
}
