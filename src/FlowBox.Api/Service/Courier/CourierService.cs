using FlowBox.Api.Repositories.Courier;

namespace FlowBox.Api.Service.Courier;

public class CourierService(ICourierRepository repository) : IService
{
    public async Task<Data.Ef.Models.Courier> CreateAsync(string name, string phone, CancellationToken ct)
    {
        var courier = new Data.Ef.Models.Courier { Name = name, Phone = phone };
        await repository.AddAsync(courier, ct);
        return courier;
    }
}
