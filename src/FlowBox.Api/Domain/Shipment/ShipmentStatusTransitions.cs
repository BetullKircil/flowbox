using FlowBox.Api.Enums;

namespace FlowBox.Api.Domain.Shipment;

public static class ShipmentStatusTransitions
{
    private static readonly ShipmentStatus[] HappyPathOrder =
    [
        ShipmentStatus.Created,
        ShipmentStatus.PickedUp,
        ShipmentStatus.ArrivedAtSortingCenter,
        ShipmentStatus.Sorted,
        ShipmentStatus.InTransit,
        ShipmentStatus.ArrivedAtDistributionCenter,
        ShipmentStatus.OutForDelivery,
        ShipmentStatus.Delivered
    ];

    private static readonly HashSet<ShipmentStatus> TerminalStatuses =
    [
        ShipmentStatus.Delivered,
        ShipmentStatus.Failed
    ];

    public static bool IsTerminal(ShipmentStatus status) => TerminalStatuses.Contains(status);

    public static bool IsValidTransition(ShipmentStatus from, ShipmentStatus to)
    {
        if (IsTerminal(from))
        {
            return false;
        }

        if (to == ShipmentStatus.Failed)
        {
            return true;
        }

        var fromIndex = Array.IndexOf(HappyPathOrder, from);
        var toIndex = Array.IndexOf(HappyPathOrder, to);

        return fromIndex >= 0 && toIndex == fromIndex + 1;
    }
    
    public static IReadOnlyCollection<ShipmentStatus> GetValidNextStatuses(ShipmentStatus from)
    {
        if (IsTerminal(from))
        {
            return [];
        }

        var fromIndex = Array.IndexOf(HappyPathOrder, from);
        var next = new List<ShipmentStatus> { ShipmentStatus.Failed };

        if (fromIndex >= 0 && fromIndex + 1 < HappyPathOrder.Length)
        {
            next.Insert(0, HappyPathOrder[fromIndex + 1]);
        }

        return next;
    }
}
