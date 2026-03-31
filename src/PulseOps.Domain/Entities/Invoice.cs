namespace PulseOps.Domain.Entities;

public class Invoice
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid OrderId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime IssuedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAtUtc { get; set; }

    public Business Business { get; set; } = null!;
    public Order Order { get; set; } = null!;
}