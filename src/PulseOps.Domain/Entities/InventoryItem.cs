namespace PulseOps.Domain.Entities;

public class InventoryItem
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public int QuantityOnHand { get; set; }
    public int ReservedQuantity { get; set; }
    public int ReorderThreshold { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public Product Product { get; set; } = null!;
}