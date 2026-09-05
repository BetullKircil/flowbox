using FlowBox.Api.Enums;

namespace FlowBox.Api.Models;

public class Shipment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TrackingNumber { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public ShipmentStatus Status { get; set; } = ShipmentStatus.Created;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<ShipmentAssignment> Assignments { get; set; } = new List<ShipmentAssignment>();
}