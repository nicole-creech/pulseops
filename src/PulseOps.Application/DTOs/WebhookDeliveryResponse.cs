namespace PulseOps.Application.DTOs;

public class WebhookDeliveryResponse
{
    public Guid Id { get; set; }
    public Guid DomainEventId { get; set; }
    public Guid WebhookEndpointId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int AttemptCount { get; set; }
    public int? ResponseStatusCode { get; set; }
    public string? ResponseBody { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? LastAttemptAtUtc { get; set; }
    public DateTime? DeliveredAtUtc { get; set; }
}