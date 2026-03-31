namespace PulseOps.Domain.Entities;

public class DomainEvent
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public bool Processed { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }

    public Business Business { get; set; } = null!;
    public ICollection<WebhookDelivery> WebhookDeliveries { get; set; } = new List<WebhookDelivery>();
}