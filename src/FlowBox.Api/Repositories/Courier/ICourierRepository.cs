using FlowBox.Api.Data.Ef.Models;

namespace FlowBox.Api.Repositories.Courier;

public interface ICourierRepository
{
    Task<Data.Ef.Models.Courier?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(Data.Ef.Models.Courier courier, CancellationToken ct);
}
