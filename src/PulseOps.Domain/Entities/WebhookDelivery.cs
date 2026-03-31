namespace PulseOps.Domain.Entities;

public class WebhookDelivery
{
    public Guid Id { get; set; }
    public Guid DomainEventId { get; set; }
    public Guid WebhookEndpointId { get; set; }
    public string Status { get; set; } = "Pending";
    public int AttemptCount { get; set; } = 0;
    public int MaxAttempts { get; set; } = 4;
    public int? ResponseStatusCode { get; set; }
    public string? ResponseBody { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastAttemptAtUtc { get; set; }
    public DateTime? NextRetryAtUtc { get; set; }
    public DateTime? DeliveredAtUtc { get; set; }

    public DomainEvent DomainEvent { get; set; } = null!;
    public WebhookEndpoint WebhookEndpoint { get; set; } = null!;
}