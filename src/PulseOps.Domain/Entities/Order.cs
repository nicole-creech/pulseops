namespace PulseOps.Domain.Entities;

public class Order
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid CustomerId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Business Business { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public Invoice? Invoice { get; set; }
}