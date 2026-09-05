namespace FlowBox.Api.Models;

public class Courier
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public required string Phone { get; set; }
    public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
}