namespace PulseOps.Domain.Entities;

public class WebhookEndpoint
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Business Business { get; set; } = null!;
    public ICollection<WebhookDelivery> WebhookDeliveries { get; set; } = new List<WebhookDelivery>();
}