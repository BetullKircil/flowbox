namespace FlowBox.Api.Enums;

/// <summary>
/// Bugün için Order, oluşturulduğu anda senkron olarak Shipment'e dönüştürülüyor,
/// bu yüzden pratikte hep Placed kalıyor. Cancelled, Shipment üretilmeden önce
/// iptal edilebilme ihtimaline karşı şimdiden ekleniyor.
/// </summary>
public enum OrderStatus
{
    Placed,
    Cancelled
}
