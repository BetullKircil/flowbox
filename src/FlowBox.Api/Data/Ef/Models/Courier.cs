namespace FlowBox.Api.Data.Ef.Models;

public class Courier
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public required string Phone { get; set; }
    public ICollection<ShipmentAssignment> Assignments { get; set; } = new List<ShipmentAssignment>();
}
