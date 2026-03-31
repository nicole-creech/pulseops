namespace PulseOps.Application.DTOs;

public class DomainEventResponse
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; }
    public bool Processed { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
}