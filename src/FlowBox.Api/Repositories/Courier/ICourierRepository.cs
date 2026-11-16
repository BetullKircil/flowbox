namespace FlowBox.Api.Repositories.Courier;

public interface ICourierRepository
{
    Task<Data.Ef.Models.Courier?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Data.Ef.Models.Courier?> GetByUserIdAsync(Guid userId, CancellationToken ct);
}
