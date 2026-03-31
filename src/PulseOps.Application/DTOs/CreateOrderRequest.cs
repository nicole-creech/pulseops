namespace PulseOps.Application.DTOs;

public class CreateOrderRequest
{
    public Guid BusinessId { get; set; }
    public Guid CustomerId { get; set; }
    public List<CreateOrderItemRequest> Items { get; set; } = new();
}