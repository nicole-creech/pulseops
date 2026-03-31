namespace PulseOps.Application.DTOs;

public class AdjustInventoryRequest
{
    public Guid ProductId { get; set; }
    public int Adjustment { get; set; }
    public string Reason { get; set; } = string.Empty;
}