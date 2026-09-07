namespace FlowBox.Api.Repositories.Order;

public interface IOrderRepository
{
    Task AddAsync(Data.Ef.Models.Order order, CancellationToken ct);
}
