using FlowBox.Api.Enums;

namespace FlowBox.Api.Data.Ef.Models;

/// <summary>
/// Müşterinin oluşturduğu ilk talep. Bugün her Order, oluşturulduğu anda
/// senkron olarak bir Shipment'e dönüştürülüyor (1:1) — ileride ödeme onayı
/// gibi bir adım araya girdiğinde bu dönüşüm asenkron hale gelecek, Order'ı
/// Shipment'ten ayrı bir entity yapmamızın sebebi tam olarak bu.
/// </summary>
public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Placed;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Shipment? Shipment { get; set; }
}
