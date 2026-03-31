namespace PulseOps.Application.DTOs;

public class CreateProductRequest
{
    public Guid BusinessId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int InitialQuantity { get; set; }
    public int ReorderThreshold { get; set; }
}