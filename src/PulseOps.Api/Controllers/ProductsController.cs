using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PulseOps.Application.DTOs;
using PulseOps.Domain.Entities;
using PulseOps.Infrastructure.Persistence;

namespace PulseOps.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly PulseOpsDbContext _dbContext;

    public ProductsController(PulseOpsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductResponse>>> GetProducts(CancellationToken cancellationToken)
    {
        var products = await _dbContext.Products
            .Include(x => x.InventoryItem)
            .Select(x => new ProductResponse
            {
                Id = x.Id,
                BusinessId = x.BusinessId,
                Name = x.Name,
                Sku = x.Sku,
                Price = x.Price,
                IsActive = x.IsActive,
                QuantityOnHand = x.InventoryItem != null ? x.InventoryItem.QuantityOnHand : 0,
                ReservedQuantity = x.InventoryItem != null ? x.InventoryItem.ReservedQuantity : 0,
                ReorderThreshold = x.InventoryItem != null ? x.InventoryItem.ReorderThreshold : 0
            })
            .ToListAsync(cancellationToken);

        return Ok(products);
    }

    [HttpPost]
    public async Task<ActionResult<ProductResponse>> CreateProduct(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var businessExists = await _dbContext.Businesses
            .AnyAsync(x => x.Id == request.BusinessId, cancellationToken);

        if (!businessExists)
        {
            return BadRequest("Business does not exist.");
        }

        var skuExists = await _dbContext.Products
            .AnyAsync(x => x.BusinessId == request.BusinessId && x.Sku == request.Sku, cancellationToken);

        if (skuExists)
        {
            return Conflict("A product with that SKU already exists for this business.");
        }

        var product = new Product
        {
            Id = Guid.NewGuid(),
            BusinessId = request.BusinessId,
            Name = request.Name.Trim(),
            Sku = request.Sku.Trim(),
            Price = request.Price,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var inventoryItem = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            QuantityOnHand = request.InitialQuantity,
            ReservedQuantity = 0,
            ReorderThreshold = request.ReorderThreshold,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Products.Add(product);
        _dbContext.InventoryItems.Add(inventoryItem);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new ProductResponse
        {
            Id = product.Id,
            BusinessId = product.BusinessId,
            Name = product.Name,
            Sku = product.Sku,
            Price = product.Price,
            IsActive = product.IsActive,
            QuantityOnHand = inventoryItem.QuantityOnHand,
            ReservedQuantity = inventoryItem.ReservedQuantity,
            ReorderThreshold = inventoryItem.ReorderThreshold
        };

        return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, response);
    }

    [HttpGet("{id:guid}")]
public async Task<ActionResult<ProductResponse>> GetProductById(Guid id, CancellationToken cancellationToken)
{
    var product = await _dbContext.Products
        .Include(x => x.InventoryItem)
        .Where(x => x.Id == id)
        .Select(x => new ProductResponse
        {
            Id = x.Id,
            BusinessId = x.BusinessId,
            Name = x.Name,
            Sku = x.Sku,
            Price = x.Price,
            IsActive = x.IsActive,
            QuantityOnHand = x.InventoryItem != null ? x.InventoryItem.QuantityOnHand : 0,
            ReservedQuantity = x.InventoryItem != null ? x.InventoryItem.ReservedQuantity : 0,
            ReorderThreshold = x.InventoryItem != null ? x.InventoryItem.ReorderThreshold : 0
        })
        .FirstOrDefaultAsync(cancellationToken);

    if (product is null)
    {
        return NotFound();
    }

    return Ok(product);
}
}