namespace PulseOps.Domain.Entities;

public class IdempotencyRecord
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string ResponseJson { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Business Business { get; set; } = null!;
}