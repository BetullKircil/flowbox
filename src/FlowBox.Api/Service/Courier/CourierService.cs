using FlowBox.Api.Repositories.Courier;
using FlowBox.Api.Repositories.Shipment;

namespace FlowBox.Api.Service.Courier;

public class CourierService(
    ICourierRepository courierRepository,
    IShipmentRepository shipmentRepository) : IService
{
    public async Task<IReadOnlyList<Data.Ef.Models.Shipment>> GetMyShipmentsAsync(
        Guid userId, int skip, int take, CancellationToken ct)
    {
        var courier = await courierRepository.GetByUserIdAsync(userId, ct);
        if (courier is null)
        {
            return [];
        }

        return await shipmentRepository.GetActiveByCourierAsync(courier.Id, skip, take, ct);
    }
}
