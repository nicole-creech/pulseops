using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PulseOps.Application.DTOs;
using PulseOps.Infrastructure.Persistence;

namespace PulseOps.Api.Controllers;

[ApiController]
[Route("api/inventory")]
public class InventoryController : ControllerBase
{
    private readonly PulseOpsDbContext _dbContext;

    public InventoryController(PulseOpsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpPost("adjust")]
    public async Task<IActionResult> AdjustInventory(
        [FromBody] AdjustInventoryRequest request,
        CancellationToken cancellationToken)
    {
        var inventoryItem = await _dbContext.InventoryItems
            .Include(x => x.Product)
            .FirstOrDefaultAsync(x => x.ProductId == request.ProductId, cancellationToken);

        if (inventoryItem is null)
        {
            return NotFound("Inventory record not found for this product.");
        }

        var newQuantity = inventoryItem.QuantityOnHand + request.Adjustment;

        if (newQuantity < 0)
        {
            return BadRequest("Inventory cannot go below zero.");
        }

        inventoryItem.QuantityOnHand = newQuantity;
        inventoryItem.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            productId = inventoryItem.ProductId,
            productName = inventoryItem.Product.Name,
            quantityOnHand = inventoryItem.QuantityOnHand,
            reservedQuantity = inventoryItem.ReservedQuantity,
            reorderThreshold = inventoryItem.ReorderThreshold,
            reason = request.Reason
        });
    }
}