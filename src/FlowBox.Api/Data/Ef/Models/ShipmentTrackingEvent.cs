using FlowBox.Api.Enums;

namespace FlowBox.Api.Data.Ef.Models;

public class ShipmentTrackingEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ShipmentId { get; set; }
    public Shipment? Shipment { get; set; }

    public ShipmentStatus Status { get; set; }
    public string? Location { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
