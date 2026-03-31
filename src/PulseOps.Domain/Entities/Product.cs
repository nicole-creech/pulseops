namespace PulseOps.Domain.Entities;

public class Product
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Business Business { get; set; } = null!;
    public InventoryItem? InventoryItem { get; set; }
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}