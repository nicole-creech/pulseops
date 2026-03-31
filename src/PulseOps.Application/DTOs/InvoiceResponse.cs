namespace PulseOps.Application.DTOs;

public class InvoiceResponse
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid OrderId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime IssuedAtUtc { get; set; }
    public DateTime? PaidAtUtc { get; set; }
}