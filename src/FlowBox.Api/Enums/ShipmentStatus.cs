namespace FlowBox.Api.Enums;

/// <summary>
/// Bir kargonun hayat döngüsündeki durumu.
/// Sıralama, gerçek operasyon akışını yansıtır:
/// Created -> PickedUp -> ArrivedAtSortingCenter -> Sorted -> InTransit
///   -> ArrivedAtDistributionCenter -> OutForDelivery -> Delivered
/// Failed, terminal olmayan herhangi bir durumdan ulaşılabilir bir istisna dalıdır.
/// </summary>
public enum ShipmentStatus
{
    Created,
    PickedUp,
    ArrivedAtSortingCenter,
    Sorted,
    InTransit,
    ArrivedAtDistributionCenter,
    OutForDelivery,
    Delivered,
    Failed
}
