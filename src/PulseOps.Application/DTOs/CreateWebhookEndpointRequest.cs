namespace PulseOps.Application.DTOs;

public class CreateWebhookEndpointRequest
{
    public Guid BusinessId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}