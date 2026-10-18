namespace FlowBox.Api.Repositories.Order;

public interface IOrderRepository
{
    Task AddAsync(Data.Ef.Models.Order order, CancellationToken ct);

    Task<IReadOnlyList<Data.Ef.Models.Order>> GetByCustomerAsync(Guid customerId, int skip, int take, CancellationToken ct);
}
