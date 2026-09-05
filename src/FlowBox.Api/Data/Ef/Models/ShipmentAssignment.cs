namespace FlowBox.Api.Data.Ef.Models;

public class ShipmentAssignment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ShipmentId { get; set; }
    public Shipment? Shipment { get; set; }

    public Guid CourierId { get; set; }
    public Courier? Courier { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public bool IsActive { get; set; } = true;
}
